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
            get { return GetValue(SelectedItemProperty); }
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
                    AppendItems(e.NewItems);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    break;
                case NotifyCollectionChangedAction.Replace:
                    break;
                case NotifyCollectionChangedAction.Move:
                    break;
                case NotifyCollectionChangedAction.Reset:
                    Items.Clear();
                    AppendItems(HierarchicalItems);
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
                        AppendItem(i);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void AppendItem(object item) => InsertItem(Items.Count, item);
        private void AppendItems(IList items) => InsertItems(Items.Count, items);

        private int InsertItem(int index, object item)
        {
            var count = 1;
            Items.Insert(index, new VirtualTreeViewItemHolder(item));
            item.IfType<VirtualTreeViewItem>(i =>
            {
                if (i.IsExpanded)
                    count += InsertItems(index + 1, i.Items);
                i.Items.IfType<INotifyCollectionChanged>(c => c.CollectionChanged += (sender, args) => OnItemItemsCollectionChanged(i, args));
            });
            return count;
        }

        private int InsertItems(int index, IList items)
        {
            var startIndex = index;
            foreach (var i in items)
                index += InsertItem(index, i);
            return index - startIndex;
        }

        private void DeleteItems(int index, int count)
        {
            while (count-- > 0)
                Items.RemoveAt(index);
        }

        private void OnItemItemsCollectionChanged(VirtualTreeViewItem item, NotifyCollectionChangedEventArgs e)
        {
            if (!item.IsExpanded)
                return;
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    InsertItems(GetInsertIndex(item, e.NewStartingIndex), e.NewItems);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    break;
                case NotifyCollectionChangedAction.Replace:
                    break;
                case NotifyCollectionChangedAction.Move:
                    break;
                case NotifyCollectionChangedAction.Reset:
                    InsertItems(GetItemIndex(item) + 1, item.Items);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        internal void OnExpanded(ItemsControl item)
        {
            var itemIndex = GetItemIndex(item);
            InsertItems(itemIndex + 1, item.Items);
        }

        internal void OnCollapsed(ItemsControl item)
        {
            var itemIndex = GetItemIndex(item);
            var lastChildIndex = GetLastChildIndex(item, false);
            DeleteItems(itemIndex + 1, lastChildIndex - itemIndex);
        }

        /// <summary>
        /// Gets the index where an item can be insertedn given the parent and child index.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="childIndex">Index of the child.</param>
        /// <returns></returns>
        private int GetInsertIndex(ItemsControl item, int childIndex)
        {
            return GetLastChildIndex(item.Items[childIndex], true);
        }

        /// <summary>
        /// Gets the last child index.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="onlyVisible">if set to <c>true</c> [only visible].</param>
        /// <returns></returns>
        private int GetLastChildIndex(object item, bool onlyVisible)
        {
            var treeViewItem = item as VirtualTreeViewItem;
            if (treeViewItem == null || treeViewItem.Items.Count == 0 || (onlyVisible && !treeViewItem.IsExpanded))
                return GetItemIndex(item);

            return GetLastChildIndex(treeViewItem.Items[treeViewItem.Items.Count - 1], onlyVisible);
        }

        /// <summary>
        /// Gets the index of the item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns></returns>
        private int GetItemIndex(object item)
        {
            // TODO: optimize to dictionary
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
