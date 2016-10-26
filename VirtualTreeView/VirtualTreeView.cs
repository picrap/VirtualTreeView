// VirtualTreeView - a TreeView that *actually* allows virtualization
// https://github.com/picrap/VirtualTreeView

namespace VirtualTreeView
{
    using System;
    using System.Collections;
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
        public static readonly DependencyProperty HierarchicalItemsSourceProperty = DependencyProperty.Register(
            nameof(HierarchicalItemsSource), typeof(TreeViewItemCollection), typeof(VirtualTreeView), new PropertyMetadata(null, OnHierarchicalItemsSourceChanged));

        public TreeViewItemCollection HierarchicalItemsSource
        {
            get { return (TreeViewItemCollection)GetValue(HierarchicalItemsSourceProperty); }
            set { SetValue(HierarchicalItemsSourceProperty, value); }
        }

        public IList HierarchicalItems { get; } = new ObservableCollection<object>();

        public bool IsSelectionChangeActive { get; set; }

        public static readonly DependencyProperty SelectedItemProperty = DependencyProperty.Register(
            "SelectedItem", typeof(object), typeof(VirtualTreeView), new PropertyMetadata(default(object)));

        public object SelectedItem
        {
            get { return (object)GetValue(SelectedItemProperty); }
            set { SetValue(SelectedItemProperty, value); }
        }

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
            // mark items
            HierarchicalItems.IfType<INotifyCollectionChanged>(nc => nc.OnAddRemove(o => o.IfType<VirtualTreeViewItem>(i => i.ParentTreeView = this)));
            // propagate changes
            HierarchicalItems.IfType<INotifyCollectionChanged>(nc => nc.CollectionChanged += OnHierarchicalItemsCollectionChanged);
        }

        private static void OnHierarchicalItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var @this = (VirtualTreeView)d;
            e.OldValue.IfType<INotifyCollectionChanged>(nc => nc.CollectionChanged -= @this.OnHierarchicalItemsSourceCollectionChanged);
            e.NewValue.IfType<INotifyCollectionChanged>(nc => nc.CollectionChanged += @this.OnHierarchicalItemsSourceCollectionChanged);
        }

        private void OnHierarchicalItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (HierarchicalItemsSource != null)
                throw new InvalidOperationException("HierarchicalItemsSource is data bound, do no use HierarchicalItems");
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    AppendRange(e.NewItems);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    break;
                case NotifyCollectionChangedAction.Replace:
                    break;
                case NotifyCollectionChangedAction.Move:
                    break;
                case NotifyCollectionChangedAction.Reset:
                    Items.Clear();
                    AppendRange(HierarchicalItems);
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
                    break;
                case NotifyCollectionChangedAction.Replace:
                    break;
                case NotifyCollectionChangedAction.Move:
                    break;
                case NotifyCollectionChangedAction.Reset:
                    Items.Clear();
                    foreach (var i in HierarchicalItemsSource)
                        Append(i);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void Append(object o) => Add(Items.Count, o);
        private void AppendRange(IList l) => AddRange(Items.Count, l);

        private int Add(int index, object o)
        {
            var count = 1;
            Items.Insert(index, new VirtualTreeViewItemHolder(o));
            o.IfType<VirtualTreeViewItem>(i =>
            {
                if (i.IsExpanded)
                    count += Add(index + 1, i.Items);
                i.Items.IfType<INotifyCollectionChanged>(c => c.CollectionChanged += (sender, args) => OnItemItemsCollectionChanged(i, args));
            });
            return count;
        }

        private void OnItemItemsCollectionChanged(VirtualTreeViewItem item, NotifyCollectionChangedEventArgs e)
        {
            if (!item.IsExpanded)
                return;
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    AddRange(GetInsertIndex(item, e.NewStartingIndex), e.NewItems);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    break;
                case NotifyCollectionChangedAction.Replace:
                    break;
                case NotifyCollectionChangedAction.Move:
                    break;
                case NotifyCollectionChangedAction.Reset:
                    AddRange(GetItemIndex(item) + 1, item.Items);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private int AddRange(int index, IList l)
        {
            var startIndex = index;
            foreach (var i in l)
                index += Add(index, i);
            return index - startIndex;
        }

        internal void OnExpanded(VirtualTreeViewItem item)
        {
            var itemIndex = GetItemIndex(item);
            AddRange(itemIndex + 1, item.Items);
        }

        internal void OnCollapsed(VirtualTreeViewItem item)
        {
        }

        private int GetInsertIndex(VirtualTreeViewItem item, int childIndex)
        {
            return GetLastChildIndex(item.Items[childIndex]);
        }

        private int GetLastChildIndex(object o)
        {
            var item = o as VirtualTreeViewItem;
            if (item == null || item.Items.Count == 0 || !item.IsExpanded)
                return GetItemIndex(o);

            return GetLastChildIndex(item.Items[item.Items.Count - 1]);
        }

        private int GetItemIndex(object item)
        {
            //return Items.IndexOf(item);
            for (int index = 0; index < Items.Count; index++)
            {
                var indexedItem = (VirtualTreeViewItemHolder)Items[index];
                if (ReferenceEquals(indexedItem.Content, item))
                    return index;
            }
            return -1;
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
