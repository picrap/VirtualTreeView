// VirtualTreeView - a TreeView that *actually* allows virtualization
// https://github.com/picrap/VirtualTreeView

namespace VirtualTreeView
{
    using System.Collections;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using System.Windows.Markup;
    using Collection;
    using Reflection;

    [StyleTypedProperty(Property = nameof(ItemContainerStyle), StyleTargetType = typeof(TreeViewItem))]
    [ContentProperty(nameof(HierarchicalItems))]
    public class VirtualTreeView : ItemsControl
    {
        private readonly TreeViewItemCollection _hierarchicalItemsSource = new TreeViewItemCollection();

        private bool _hierarchicalItemsSourceBound;

        public IEnumerable HierarchicalItemsSource
        {
            get { return _hierarchicalItemsSource; }
            set
            {
                _hierarchicalItemsSource.Clear();
                _hierarchicalItemsSourceBound = value != null;
                if (_hierarchicalItemsSourceBound)
                {
                    // on first binding, create the collection
                    if (FlatItemsSource == null)
                    {
                        var itemsSource = new ObservableCollection<object>();
                        FlatItemsSource = new VirtualTreeViewItemsSourceFlatCollection(_hierarchicalItemsSource, itemsSource, this);
                        ItemsSource = itemsSource;
                    }
                    if (IsLoaded)
                    {
                        foreach (var newItem in value)
                            _hierarchicalItemsSource.Add(newItem);
                    }
                    else
                        Loaded += delegate
                        {
                            foreach (var newItem in value)
                                _hierarchicalItemsSource.Add(newItem);
                        };
                }
            }
        }

        public IList HierarchicalItems { get; } = new ObservableCollection<object>();

        public bool IsSelectionChangeActive { get; set; }

        public static readonly DependencyProperty SelectedItemProperty = DependencyProperty.Register(
            "SelectedItem", typeof(object), typeof(VirtualTreeView), new PropertyMetadata(default(object)));

        public object SelectedItem
        {
            get { return GetValue(SelectedItemProperty); }
            set { SetValue(SelectedItemProperty, value); }
        }

        private VirtualTreeViewItemFlatCollection FlatItems { get; }
        private VirtualTreeViewItemsSourceFlatCollection FlatItemsSource { get; set; }

        /// <summary>
        ///     Event fired when <see cref="SelectedItem"/> changes.
        /// </summary>
        public static readonly RoutedEvent SelectedItemChangedEvent
            = EventManager.RegisterRoutedEvent("SelectedItemChanged", RoutingStrategy.Bubble, typeof(RoutedPropertyChangedEventHandler<object>), typeof(VirtualTreeView));

        /// <summary>
        ///     Event fired when <see cref="SelectedItem"/> changes.
        /// </summary>
        [Category("Behavior")]
        public event RoutedPropertyChangedEventHandler<object> SelectedItemChanged
        {
            add { AddHandler(SelectedItemChangedEvent, value); }
            remove { RemoveHandler(SelectedItemChangedEvent, value); }
        }

        /// <summary>
        ///     Called when <see cref="SelectedItem"/> changes.
        ///     Default implementation fires the <see cref="SelectedItemChanged"/> event.
        /// </summary>
        /// <param name="e">Event arguments.</param>
        protected virtual void OnSelectedItemChanged(RoutedPropertyChangedEventArgs<object> e)
        {
            RaiseEvent(e);
        }

        static VirtualTreeView()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(VirtualTreeView), new FrameworkPropertyMetadata(typeof(VirtualTreeView)));
        }

        public VirtualTreeView()
        {
            FlatItems = new VirtualTreeViewItemFlatCollection(HierarchicalItems, Items);
            //FlatItemsSource=new VirtualTreeViewItemsSourceFlatCollection(HierarchicalItemsSource,);
            // mark items
            HierarchicalItems.IfType<INotifyCollectionChanged>(nc => nc.OnAddRemove(o => o.IfType<VirtualTreeViewItem>(i => i.ParentTreeView = this)));
            // propagate changes
            //HierarchicalItems.IfType<INotifyCollectionChanged>(nc => nc.CollectionChanged += OnHierarchicalItemsCollectionChanged);
            //_hierarchicalItemsSource.CollectionChanged += OnHierarchicalItemsSourceCollectionChanged;
        }

        internal void OnExpanded(ItemsControl item)
        {
            if (_hierarchicalItemsSourceBound)
                FlatItemsSource.Expand(item.DataContext);
            else
                FlatItems.Expand(item);
        }

        internal void OnCollapsed(ItemsControl item)
        {
            if (_hierarchicalItemsSourceBound)
                FlatItemsSource.Collapse(item.DataContext);
            else
                FlatItems.Collapse(item);
        }

        private VirtualTreeViewItem _selectedContainer;

        internal void ChangeSelection(object data, VirtualTreeViewItem container, bool selected)
        {
            if (IsSelectionChangeActive)
            {
                return;
            }

            object oldValue = null;
            object newValue = null;
            bool changed = false;

            IsSelectionChangeActive = true;

            try
            {
                if (selected)
                {
                    if (container != _selectedContainer)
                    {
                        oldValue = SelectedItem;
                        newValue = data;

                        if (_selectedContainer != null)
                        {
                            _selectedContainer.IsSelected = false;
                            _selectedContainer.UpdateContainsSelection(false);
                        }
                        _selectedContainer = container;
                        _selectedContainer.UpdateContainsSelection(true);
                        SelectedItem = data;
                        //UpdateSelectedValue(data);
                        changed = true;
                    }
                }
                else
                {
                    if (container == _selectedContainer)
                    {
                        _selectedContainer.UpdateContainsSelection(false);
                        _selectedContainer = null;
                        SelectedItem = null;

                        oldValue = data;
                        changed = true;
                    }
                }

                if (container.IsSelected != selected)
                    container.IsSelected = selected;
            }
            finally
            {
                IsSelectionChangeActive = false;
            }

            if (changed)
            {
                var e = new RoutedPropertyChangedEventArgs<object>(oldValue, newValue, SelectedItemChangedEvent);
                OnSelectedItemChanged(e);
            }
        }

        protected override DependencyObject GetContainerForItemOverride()
        {
            return new VirtualTreeViewItem { ParentTreeView = this };
        }

        internal int GetDepth(VirtualTreeViewItem treeViewItem)
        {
            int depth = -1; // starting from -1 here, cause the dataContext below will be non-null at least once
            for (var dataContext = treeViewItem.DataContext; dataContext != null; dataContext = FlatItemsSource.GetParent(dataContext))
                depth++;
            return depth;
        }

        internal VirtualTreeViewItem GetContainer(object item)
        {
            // most efficient: get from view
            var treeViewItem = (VirtualTreeViewItem)ItemContainerGenerator.ContainerFromItem(item);
            if (treeViewItem != null)
                return treeViewItem;

            return new VirtualTreeViewItem { DataContext = item };

            // but at early stages, the container might not been generated
            IItemContainerGenerator generator = ItemContainerGenerator;
            var index = _hierarchicalItemsSource.IndexOf(item);
            var pos = generator.GeneratorPositionFromIndex(-1);
            using (generator.StartAt(pos, GeneratorDirection.Forward))
            {
                bool isNewlyRealized;
                var container = generator.GenerateNext(out isNewlyRealized);
                if (isNewlyRealized)
                    generator.PrepareItemContainer(container);
            }

            treeViewItem = (VirtualTreeViewItem)ItemContainerGenerator.ContainerFromItem(item);
            return treeViewItem;
        }
    }
}
