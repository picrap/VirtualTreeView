// VirtualTreeView - a TreeView that *actually* allows virtualization
// https://github.com/picrap/VirtualTreeView

namespace VirtualTreeView
{
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using Collection;

    /// <summary>
    /// Base item for <see cref="VirtualTreeView"/>
    /// </summary>
    /// <seealso cref="System.Windows.Controls.HeaderedItemsControl" />
    public class VirtualTreeViewItem : HeaderedItemsControl
    {
        /// <summary>
        /// The is expanded property
        /// </summary>
        public static readonly DependencyProperty IsExpandedProperty
            = DependencyProperty.Register("IsExpanded", typeof(bool), typeof(VirtualTreeViewItem), new PropertyMetadata(default(bool), OnIsExpandedChanged));

        /// <summary>
        /// Gets or sets a value indicating whether this instance is expanded.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is expanded; otherwise, <c>false</c>.
        /// </value>
        public bool IsExpanded
        {
            get { return (bool)GetValue(IsExpandedProperty); }
            set { SetValue(IsExpandedProperty, value); }
        }

        /// <summary>
        /// The is selected property
        /// </summary>
        public static readonly DependencyProperty IsSelectedProperty
            = DependencyProperty.Register("IsSelected", typeof(bool), typeof(VirtualTreeViewItem), new PropertyMetadata(default(bool),
                (d, e) => ((VirtualTreeViewItem)d).OnIsSelectedChanged((bool)e.NewValue)));

        /// <summary>
        /// Gets or sets a value indicating whether this instance is selected.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is selected; otherwise, <c>false</c>.
        /// </value>
        public bool IsSelected
        {
            get { return (bool)GetValue(IsSelectedProperty); }
            set { SetValue(IsSelectedProperty, value); }
        }

        /// <summary>
        /// The is selection active property
        /// </summary>
        public static readonly DependencyProperty IsSelectionActiveProperty
            = DependencyProperty.Register("IsSelectionActive", typeof(bool), typeof(VirtualTreeViewItem), new PropertyMetadata(default(bool)));

        /// <summary>
        /// Gets or sets a value indicating whether this instance is selection active.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is selection active; otherwise, <c>false</c>.
        /// </value>
        public bool IsSelectionActive
        {
            get { return (bool)GetValue(IsSelectionActiveProperty); }
            set { SetValue(IsSelectionActiveProperty, value); }
        }

        /// <summary>
        /// The level margin property
        /// </summary>
        public static readonly DependencyProperty LevelMarginProperty
            = DependencyProperty.Register("LevelMargin", typeof(double), typeof(VirtualTreeViewItem), new PropertyMetadata(default(double)));

        /// <summary>
        /// Gets or sets the level margin.
        /// This margin is multiplied by depth level to get left margin before actual item header
        /// </summary>
        /// <value>
        /// The level margin.
        /// </value>
        public double LevelMargin
        {
            get { return (double)GetValue(LevelMarginProperty); }
            set { SetValue(LevelMarginProperty, value); }
        }

        private VirtualTreeView _parentTreeView;

        /// <summary>
        ///     Walks up the parent chain of TreeViewItems to the top TreeView.
        /// </summary>
        internal VirtualTreeView ParentTreeView
        {
            get { return _parentTreeView; }
            set
            {
                _parentTreeView = value;
                Header = new Grid();
            }
        }

        /// <summary>
        ///     Returns the immediate parent VirtualTreeViewItem. Null if the parent is a TreeView.
        /// </summary>
        internal VirtualTreeViewItem ParentTreeViewItem { get; set; }

        private int? _depth;

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
                if (!_depth.HasValue)
                {
                    int depth = 0;
                    for (var parent = ParentTreeViewItem; parent != null; parent = parent.ParentTreeViewItem)
                        depth++;
                    _depth = depth;
                }
                return _depth.Value;
            }
            internal set
            {
                _depth = value;
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

        /// <summary>
        /// Initializes a new instance of the <see cref="VirtualTreeViewItem"/> class.
        /// </summary>
        public VirtualTreeViewItem()
        {
            INotifyCollectionChanged notifyCollectionChanged = Items;
            notifyCollectionChanged.CollectionChanged += OnItemsCollectionChanged;
        }

        private void OnItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            foreach (var item in e.GetAddedItems(sender).OfType<VirtualTreeViewItem>())
            {
                item.ParentTreeView = ParentTreeView;
                item.ParentTreeViewItem = this;
            }
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

        /// <summary>
        ///     Called when the left mouse button is pressed down.
        /// </summary>
        /// <param name="e">Event arguments</param>
        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            if (!e.Handled && IsEnabled)
            {
                if (Focus())
                {
                    e.Handled = true;
                }

                if (e.ClickCount % 2 == 0)
                {
                    IsExpanded = !IsExpanded;
                    e.Handled = true;
                }
            }
            base.OnMouseLeftButtonDown(e);
        }

        /// <summary>
        /// Invoked whenever an unhandled <see cref="E:System.Windows.UIElement.GotFocus" /> event reaches this element in its route.
        /// </summary>
        /// <param name="e">The <see cref="T:System.Windows.RoutedEventArgs" /> that contains the event data.</param>
        protected override void OnGotFocus(RoutedEventArgs e)
        {
            ParentTreeView.Select(this, true);
            base.OnGotFocus(e);
        }

        /// <summary>
        /// Called when [is selected changed].
        /// </summary>
        /// <param name="selected">if set to <c>true</c> [selected].</param>
        private void OnIsSelectedChanged(bool selected)
        {
            var treeView = ParentTreeView;
            if (treeView == null)
                return;

            if (selected)
            {
                treeView.Select(this, false);
                OnSelected(new RoutedEventArgs(SelectedEvent, this));
            }
            else
            {
                treeView.Deselect(this);
                OnUnselected(new RoutedEventArgs(UnselectedEvent, this));
            }
        }

        /// <summary>
        /// Invoked when an unhandled <see cref="E:System.Windows.Input.Keyboard.GotKeyboardFocus" /> attached event reaches an element in its route that is derived from this class. Implement this method to add class handling for this event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.Windows.Input.KeyboardFocusChangedEventArgs" /> that contains the event data.</param>
        protected override void OnGotKeyboardFocus(KeyboardFocusChangedEventArgs e)
        {
            IsSelectionActive = true;
            base.OnGotKeyboardFocus(e);
        }

        /// <summary>
        /// Invoked when an unhandled <see cref="E:System.Windows.Input.Keyboard.LostKeyboardFocus" /> attached event reaches an element in its route that is derived from this class. Implement this method to add class handling for this event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.Windows.Input.KeyboardFocusChangedEventArgs" /> that contains event data.</param>
        protected override void OnLostKeyboardFocus(KeyboardFocusChangedEventArgs e)
        {
            IsSelectionActive = false;
            base.OnLostKeyboardFocus(e);
        }
    }
}
