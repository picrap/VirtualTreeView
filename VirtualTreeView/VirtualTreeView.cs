// VirtualTreeView - a TreeView that *actually* allows virtualization
// https://github.com/picrap/VirtualTreeView

namespace VirtualTreeView
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Windows;
    using System.Windows.Controls;
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
                ItemsSource = null;
                _hierarchicalItemsSourceBound = value != null;
                if (_hierarchicalItemsSourceBound)
                {
                    foreach (var newItem in value)
                        _hierarchicalItemsSource.Add(newItem);
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

        private VirtualTreeViewFlatCollection FlatItems { get; }
        private IndexedCollection IndexedItemsSource { get; }

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
            FlatItems = new VirtualTreeViewFlatCollection(HierarchicalItems, Items);
            IndexedItemsSource = new IndexedCollection(_hierarchicalItemsSource);
            // mark items
            HierarchicalItems.IfType<INotifyCollectionChanged>(nc => nc.OnAddRemove(o => o.IfType<VirtualTreeViewItem>(i => i.ParentTreeView = this)));
            // propagate changes
            //HierarchicalItems.IfType<INotifyCollectionChanged>(nc => nc.CollectionChanged += OnHierarchicalItemsCollectionChanged);
            _hierarchicalItemsSource.CollectionChanged += OnHierarchicalItemsSourceCollectionChanged;
        }

        private void OnHierarchicalItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            e.OldValue.IfType<INotifyCollectionChanged>(nc => nc.CollectionChanged -= OnHierarchicalItemsSourceCollectionChanged);
            e.NewValue.IfType<INotifyCollectionChanged>(nc => nc.CollectionChanged += OnHierarchicalItemsSourceCollectionChanged);
        }

        private void OnHierarchicalItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (_hierarchicalItemsSourceBound)
                throw new InvalidOperationException("HierarchicalItemsSource is data bound, do no use HierarchicalItems");
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    FlatItems.AppendItems(e.NewItems);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    throw new NotImplementedException();
                    break;
                case NotifyCollectionChangedAction.Replace:
                    throw new NotImplementedException();
                    break;
                case NotifyCollectionChangedAction.Move:
                    throw new NotImplementedException();
                    break;
                case NotifyCollectionChangedAction.Reset:
                    FlatItems.Clear();
                    FlatItems.AppendItems(HierarchicalItems);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void OnHierarchicalItemsSourceCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    break;
                case NotifyCollectionChangedAction.Remove:
                    throw new NotImplementedException();
                    break;
                case NotifyCollectionChangedAction.Replace:
                    throw new NotImplementedException();
                    break;
                case NotifyCollectionChangedAction.Move:
                    throw new NotImplementedException();
                    break;
                case NotifyCollectionChangedAction.Reset:
                    IndexedItemsSource.Clear();

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        internal void OnExpanded(ItemsControl item)
        {
            FlatItems.Expand(item);
        }

        internal void OnCollapsed(ItemsControl item)
        {
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
    }
}
