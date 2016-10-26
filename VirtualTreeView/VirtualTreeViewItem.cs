// VirtualTreeView - a TreeView that *actually* allows virtualization
// https://github.com/picrap/VirtualTreeView

namespace VirtualTreeView
{
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Windows;
    using System.Windows.Controls;
    using Collection;
    using Reflection;

    public class VirtualTreeViewItem : HeaderedItemsControl
    {
        public static readonly DependencyProperty IsExpandedProperty = DependencyProperty.Register(
            "IsExpanded", typeof(bool), typeof(VirtualTreeViewItem), new PropertyMetadata(default(bool), OnIsExpandedChanged));

        public bool IsExpanded
        {
            get { return (bool)GetValue(IsExpandedProperty); }
            set { SetValue(IsExpandedProperty, value); }
        }

        public static readonly DependencyProperty IsSelectedProperty = DependencyProperty.Register(
            "IsSelected", typeof(bool), typeof(VirtualTreeViewItem), new PropertyMetadata(default(bool), OnIsSelectedChanged));

        public bool IsSelected
        {
            get { return (bool)GetValue(IsSelectedProperty); }
            set { SetValue(IsSelectedProperty, value); }
        }

        public static readonly DependencyProperty IsSelectionActiveProperty = DependencyProperty.Register(
            "IsSelectionActive", typeof(bool), typeof(VirtualTreeViewItem), new PropertyMetadata(default(bool)));

        public bool IsSelectionActive
        {
            get { return (bool)GetValue(IsSelectionActiveProperty); }
            set { SetValue(IsSelectionActiveProperty, value); }
        }

        public static readonly DependencyProperty LevelMarginProperty = DependencyProperty.Register(
            "LevelMargin", typeof(double), typeof(VirtualTreeViewItem), new PropertyMetadata(default(double)));

        public double LevelMargin
        {
            get { return (double) GetValue(LevelMarginProperty); }
            set { SetValue(LevelMarginProperty, value); }
        }

        /// <summary>
        ///     Walks up the parent chain of TreeViewItems to the top TreeView.
        /// </summary>
        internal VirtualTreeView ParentTreeView { get; set; }

        /// <summary>
        ///     Returns the immediate parent VirtualTreeViewItem. Null if the parent is a TreeView.
        /// </summary>
        internal VirtualTreeViewItem ParentTreeViewItem { get; set; }

        /// <summary>
        /// Gets the depth.
        /// </summary>
        /// <value>
        /// The depth.
        /// </value>
        public int Depth
        {
            get
            {
                int depth = 0;
                for (var parent = ParentTreeViewItem; parent != null; parent = parent.ParentTreeViewItem)
                    depth++;
                return depth;
            }
        }

        /// <summary>
        ///     Event fired when <see cref="IsExpanded"/> becomes true.
        /// </summary>
        public static readonly RoutedEvent ExpandedEvent = EventManager.RegisterRoutedEvent("Expanded", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(VirtualTreeViewItem));

        /// <summary>
        ///     Event fired when <see cref="IsExpanded"/> becomes true.
        /// </summary>
        [Category("Behavior")]
        public event RoutedEventHandler Expanded
        {
            add { AddHandler(ExpandedEvent, value); }
            remove { RemoveHandler(ExpandedEvent, value); }
        }

        /// <summary>
        ///     Called when <see cref="IsExpanded"/> becomes true.
        ///     Default implementation fires the <see cref="Expanded"/> event.
        /// </summary>
        /// <param name="e">Event arguments.</param>
        protected virtual void OnExpanded(RoutedEventArgs e)
        {
            RaiseEvent(e);
        }

        /// <summary>
        ///     Event fired when <see cref="IsExpanded"/> becomes false.
        /// </summary>
        public static readonly RoutedEvent CollapsedEvent = EventManager.RegisterRoutedEvent("Collapsed", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(VirtualTreeViewItem));

        /// <summary>
        ///     Event fired when <see cref="IsExpanded"/> becomes false.
        /// </summary>
        [Category("Behavior")]
        public event RoutedEventHandler Collapsed
        {
            add { AddHandler(CollapsedEvent, value); }
            remove { RemoveHandler(CollapsedEvent, value); }
        }

        /// <summary>
        ///     Called when <see cref="IsExpanded"/> becomes false.
        ///     Default implementation fires the <see cref="Collapsed"/> event.
        /// </summary>
        /// <param name="e">Event arguments.</param>
        protected virtual void OnCollapsed(RoutedEventArgs e)
        {
            RaiseEvent(e);
        }

        /// <summary>
        ///     Event fired when <see cref="IsSelected"/> becomes true.
        /// </summary>
        public static readonly RoutedEvent SelectedEvent = EventManager.RegisterRoutedEvent("Selected", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(VirtualTreeViewItem));

        /// <summary>
        ///     Event fired when <see cref="IsSelected"/> becomes true.
        /// </summary>
        [Category("Behavior")]
        public event RoutedEventHandler Selected
        {
            add { AddHandler(SelectedEvent, value); }
            remove { RemoveHandler(SelectedEvent, value); }
        }

        /// <summary>
        ///     Called when <see cref="IsSelected"/> becomes true.
        ///     Default implementation fires the <see cref="Selected"/> event.
        /// </summary>
        /// <param name="e">Event arguments.</param>
        protected virtual void OnSelected(RoutedEventArgs e)
        {
            RaiseEvent(e);
        }

        /// <summary>
        ///     Event fired when <see cref="IsSelected"/> becomes false.
        /// </summary>
        public static readonly RoutedEvent UnselectedEvent = EventManager.RegisterRoutedEvent("Unselected", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(VirtualTreeViewItem));

        /// <summary>
        ///     Event fired when <see cref="IsSelected"/> becomes false.
        /// </summary>
        [Category("Behavior")]
        public event RoutedEventHandler Unselected
        {
            add { AddHandler(UnselectedEvent, value); }
            remove { RemoveHandler(UnselectedEvent, value); }
        }

        /// <summary>
        ///     Called when <see cref="IsSelected"/> becomes false.
        ///     Default implementation fires the <see cref="Unselected"/> event.
        /// </summary>
        /// <param name="e">Event arguments.</param>
        protected virtual void OnUnselected(RoutedEventArgs e)
        {
            RaiseEvent(e);
        }

        static VirtualTreeViewItem()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(VirtualTreeViewItem), new FrameworkPropertyMetadata(typeof(VirtualTreeViewItem)));
        }

        public VirtualTreeViewItem()
        {
            INotifyCollectionChanged notifyCollectionChanged = Items;
            notifyCollectionChanged.OnAddRemove(o => o.IfType<VirtualTreeViewItem>(i =>
            {
                i.ParentTreeViewItem = this;
                i.ParentTreeView = ParentTreeView;
            }));
        }

        private static void OnIsExpandedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var item = (VirtualTreeViewItem)d;
            bool isExpanded = (bool)e.NewValue;

            if (isExpanded)
                item.ParentTreeView?.OnExpanded(item);
            else
                item.ParentTreeView?.OnCollapsed(item);

            if (isExpanded)
                item.OnExpanded(new RoutedEventArgs(ExpandedEvent, item));
            else
                item.OnCollapsed(new RoutedEventArgs(CollapsedEvent, item));
        }

        private static void OnIsSelectedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var item = (VirtualTreeViewItem)d;
            bool isSelected = (bool)e.NewValue;

            item.Select(isSelected);

            if (isSelected)
                item.OnSelected(new RoutedEventArgs(SelectedEvent, item));
            else
                item.OnUnselected(new RoutedEventArgs(UnselectedEvent, item));
        }

        internal object GetItemOrContainerFromContainer(DependencyObject container)
        {
            object item = ItemContainerGenerator.ItemFromContainer(container);

            if (item == DependencyProperty.UnsetValue
                && ItemsControlFromItemContainer(container) == this
                //&& ((IGeneratorHost)this).IsItemItsOwnContainer(container)
                )
            {
                item = container;
            }

            return item;
        }


        private void Select(bool selected)
        {
            var treeView = ParentTreeView;
            var parent = ParentTreeViewItem;
            if (treeView != null && parent != null && !treeView.IsSelectionChangeActive)
            {
                // Give the TreeView a reference to this container and its data
                object data = parent.GetItemOrContainerFromContainer(this);
                treeView.ChangeSelection(data, this, selected);

                // Making focus of TreeViewItem synchronize with selection if needed.
                if (selected && treeView.IsKeyboardFocusWithin && !IsKeyboardFocusWithin)
                {
                    Focus();
                }
            }
        }
        internal void UpdateContainsSelection(bool selected)
        {
            //TreeViewItem parent = ParentTreeViewItem;
            //while (parent != null)
            //{
            //    parent.ContainsSelection = selected;
            //    parent = parent.ParentTreeViewItem;
            //}
        }
    }
}
