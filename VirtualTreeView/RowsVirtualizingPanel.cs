// VirtualTreeView - a TreeView that *actually* allows virtualization
// https://github.com/picrap/VirtualTreeView

namespace VirtualTreeView
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using System.Windows.Forms;
    using System.Windows.Interop;
    using System.Windows.Media;
    using Application = System.Windows.Application;

    /// <summary>
    /// Optimized <see cref="VirtualizingPanel"/> for rows
    /// Inspired from https://blogs.msdn.microsoft.com/dancre/2006/02/17/implementing-a-virtualizingpanel-part-4-the-goods/
    /// </summary>
    /// <seealso cref="System.Windows.Controls.VirtualizingPanel" />
    public class RowsVirtualizingPanel : VirtualizingPanel, IScrollInfo
    {
        private double _itemHeight;

        private double ItemHeight
        {
            get
            {
                if (!ValidItemHeight)
                    throw new InvalidOperationException();
                return _itemHeight;
            }
        }

        private bool _validItemHeight;

        private bool ValidItemHeight
        {
            get
            {
                CheckItemHeight();
                return _validItemHeight;
            }
        }

        private void CheckItemHeight()
        {
            if (!_validItemHeight)
            {
                // ensure one child
                RealizeChildren(0, 1);
                if (_internalChildrenByIndex.Count > 0)
                {
                    var child = _internalChildrenByIndex.Values.First();
                    child.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                    _itemHeight = child.DesiredSize.Height;
                    _validItemHeight = _itemHeight > 0;
                }
            }
        }

        private bool _externalScrollOwnerSearched;
        private ScrollViewer _externalScrollOwner;

        private ScrollViewer ExternalScrollOwner
        {
            get
            {
                if (!_externalScrollOwnerSearched)
                {
                    _externalScrollOwnerSearched = true;
                    for (DependencyObject d = this; d != null; d = VisualTreeHelper.GetParent(d))
                    {
                        var s = d as ScrollViewer;
                        if (s != null)
                        {
                            _externalScrollOwner = s;
                            s.ScrollChanged += OnExternalScrollOwnerScrollChanged;
                            s.SizeChanged += OnExternalScrollOwnerSizeChanged;
                            break;
                        }
                    }
                }
                return _externalScrollOwner;
            }
        }

        private int ItemCount
        {
            get
            {
                var itemsControl = ItemsControl.GetItemsOwner(this);
                int itemCount = itemsControl.HasItems ? itemsControl.Items.Count : 0;
                return itemCount;
            }
        }

        private DateTime? _lastActivity;

        /// <summary>
        /// Initializes a new instance of the <see cref="RowsVirtualizingPanel"/> class.
        /// </summary>
        public RowsVirtualizingPanel()
        {
            ComponentDispatcher.ThreadIdle += OnThreadIdle;
            Unloaded += delegate { ComponentDispatcher.ThreadIdle -= OnThreadIdle; };
        }

        private void OnThreadIdle(object sender, EventArgs e)
        {
          //  if (_lastActivity.HasValue && (DateTime.UtcNow - _lastActivity.Value).TotalSeconds > 2)
          //  {
          //      _lastActivity = null;

          //      var toBeRemovedElements = InternalChildren.Cast<UIElement>().Where(el => el.Visibility == Visibility.Collapsed).ToArray();
          //      var generator = GetItemContainerGenerator();
          //      foreach (var toBeRemovedElement in toBeRemovedElements)
          //      {
          //          RemoveInternalChildRange(InternalChildren.IndexOf(toBeRemovedElement), 1);
          ////          generator.Remove(generator.GeneratorPositionFromIndex());
          //      }

          //      _internalChildrenByIndex.Clear();
          //  }
        }

        private IItemContainerGenerator GetItemContainerGenerator()
        {
            // this ensures that ItemContainerGenerator will be valid
            var children = this.InternalChildren;
            var generator = ItemContainerGenerator;
            return generator;
        }

        /// <summary>
        /// Measures the size in layout required for child elements and determines a size for the <see cref="T:System.Windows.FrameworkElement" />-derived class.
        /// </summary>
        /// <param name="availableSize">The available size that this element can give to child elements. Infinity can be specified as a value to indicate that the element will size to whatever content is available.</param>
        /// <returns>
        /// The size that this element determines it needs during layout, based on its calculations of child element sizes.
        /// </returns>
        protected override Size MeasureOverride(Size availableSize)
        {
            double width;
            var itemHeight = ValidItemHeight ? ItemHeight : 0;
            var availableWidth = availableSize.Width;
            if (InternalChildren.Count > 0)
            {
                width = 0;
                foreach (UIElement child in InternalChildren)
                {
                    child.Measure(new Size(availableWidth, itemHeight));
                    width = Math.Max(width, child.DesiredSize.Width);
                }
            }
            else
            {
                width = availableWidth;
                if (double.IsInfinity(width))
                    width = ActualWidth;
            }

            var measureOverride = new Size(width, ItemCount * itemHeight);
            if (measureOverride != _extent)
            {
                _extent = measureOverride;
                ScrollOwner?.InvalidateScrollInfo();
            }

            if (ScrollOwner != null)
            {
                var viewport = new Size(ScrollOwner.ActualWidth, ScrollOwner.ActualHeight);
                if (viewport != _viewport)
                {
                    _viewport = viewport;
                    ScrollOwner.InvalidateScrollInfo();
                }
            }
            else if (ExternalScrollOwner != null)
            {
                var viewport = new Size(ExternalScrollOwner.ActualWidth, ExternalScrollOwner.ActualHeight);
                if (viewport != _viewport)
                {
                    _viewport = viewport;
                    ExternalScrollOwner.InvalidateScrollInfo();
                }
            }

            return measureOverride;
        }

        /// <summary>
        /// Positions child elements and determines a size for a <see cref="T:System.Windows.FrameworkElement" /> derived class.
        /// </summary>
        /// <param name="finalSize">The final area within the parent that this element should use to arrange itself and its children.</param>
        /// <returns>
        /// The actual size used.
        /// </returns>
        protected override Size ArrangeOverride(Size finalSize)
        {
            var visibleRange = GetVisibleRange();
            RealizeChildren(visibleRange.Item1, visibleRange.Item2);
            var remainingChildren = InternalChildren.Cast<UIElement>().ToList();
            for (int index = 0; index < visibleRange.Item2; index++)
            {
                var child = _internalChildrenByIndex[visibleRange.Item1 + index];
                child.Visibility = Visibility.Visible;
                remainingChildren.Remove(child);
                ArrangeChild(index, child, finalSize);
            }
            foreach (var remainingChild in remainingChildren)
                remainingChild.Visibility = Visibility.Collapsed;

            _lastActivity = DateTime.UtcNow;

            return finalSize;
        }

        private readonly IDictionary<int, UIElement> _internalChildrenByIndex = new Dictionary<int, UIElement>();

        /// <summary>
        /// Realizes the children, or ensures they are already realized.
        /// </summary>
        /// <param name="start">The start.</param>
        /// <param name="count">The count.</param>
        private void RealizeChildren(int start, int count)
        {
            var generator = GetItemContainerGenerator();
            var startPos = generator.GeneratorPositionFromIndex(start);
            using (generator.StartAt(startPos, GeneratorDirection.Forward, true))
            {
                for (int index = 0; index < count; index++)
                {
                    bool newlyRealized;
                    // Get or create the child
                    var child = (UIElement) generator.GenerateNext(out newlyRealized);
                    if (child == null)
                        continue;
                    if (newlyRealized)
                    {
                        // Muy important: to be bound correctly, the child must be added first
                        if (!InternalChildren.Contains(child))
                            AddInternalChild(child);
                        generator.PrepareItemContainer(child);
                    }
                    _internalChildrenByIndex[start + index] = child;
                }
            }
        }

        private Tuple<int, int> GetVisibleRange()
        {
            int itemCount = ItemCount;
            if (!ValidItemHeight)
                return Tuple.Create(0, itemCount);

            var itemHeight = ItemHeight;
            var first = Math.Max(0, (int) Math.Floor(_offset.Y / itemHeight));
            var last = Math.Min(itemCount - 1, (int) Math.Ceiling((_offset.Y + _viewport.Height) / itemHeight));

            return Tuple.Create(first, last - first + 1);
        }

        /// <summary>
        /// When items are removed, remove the corresponding UI if necessary
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        protected override void OnItemsChanged(object sender, ItemsChangedEventArgs args)
        {
            // TODO: refine :)
            // (however this should not be a problem here, since generated items are kept)
            //RemoveInternalChildRange(0, InternalChildren.Count);
            _internalChildrenByIndex.Clear();
            //switch (args.Action)
            //{
            //    case NotifyCollectionChangedAction.Remove:
            //    case NotifyCollectionChangedAction.Replace:
            //    case NotifyCollectionChangedAction.Move:
            //        RemoveInternalChildRange(args.Position.Index, args.ItemUICount);
            //        break;
            //}
        }

        /// <summary>
        /// Position a child
        /// </summary>
        /// <param name="itemIndex">The data item index of the child</param>
        /// <param name="child">The element to position</param>
        /// <param name="finalSize">The size of the panel</param>
        private void ArrangeChild(int itemIndex, UIElement child, Size finalSize)
        {
            // external scroll owner compensates the sub item-height scroll, so we have to uncompensate it
            var lineOffset = ExternalScrollOwner != null ? -Math.Floor(_offset.Y / ItemHeight) * ItemHeight : _offset.Y % ItemHeight;
            child.Arrange(new Rect(-_offset.X, itemIndex * ItemHeight - lineOffset, finalSize.Width, ItemHeight));
        }

        private void OnExternalScrollOwnerSizeChanged(object sender, SizeChangedEventArgs e)
        {
            InvalidateMeasure();
        }

        private void OnExternalScrollOwnerScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (_offset.X != e.HorizontalOffset || _offset.Y != e.VerticalOffset)
            {
                _offset.X = e.HorizontalOffset;
                _offset.Y = e.VerticalOffset;
                InvalidateMeasure();
            }
        }

        #region IScrollInfo implementation

        // See Ben Constable's series of posts at http://blogs.msdn.com/bencon/

        /// <summary>
        /// Gets or sets a <see cref="T:System.Windows.Controls.ScrollViewer" /> element that controls scrolling behavior.
        /// </summary>
        public ScrollViewer ScrollOwner { get; set; }

        /// <summary>
        /// Gets or sets a value that indicates whether scrolling on the horizontal axis is possible.
        /// </summary>
        public bool CanHorizontallyScroll { get; set; }

        /// <summary>
        /// Gets or sets a value that indicates whether scrolling on the vertical axis is possible.
        /// </summary>
        public bool CanVerticallyScroll { get; set; }

        /// <summary>
        /// Gets the horizontal offset of the scrolled content.
        /// </summary>
        public double HorizontalOffset => _offset.X;

        /// <summary>
        /// Gets the vertical offset of the scrolled content.
        /// </summary>
        public double VerticalOffset => _offset.Y;

        /// <summary>
        /// Gets the vertical size of the extent.
        /// </summary>
        public double ExtentHeight => _extent.Height;

        /// <summary>
        /// Gets the horizontal size of the extent.
        /// </summary>
        public double ExtentWidth => _extent.Width;

        /// <summary>
        /// Gets the vertical size of the viewport for this content.
        /// </summary>
        public double ViewportHeight => _viewport.Height;

        /// <summary>
        /// Gets the horizontal size of the viewport for this content.
        /// </summary>
        public double ViewportWidth => _viewport.Width;

        /// <summary>
        /// Scrolls up within content by one logical unit.
        /// </summary>
        public void LineUp()
        {
            SetVerticalOffset(VerticalOffset - ItemHeight);
        }

        /// <summary>
        /// Scrolls down within content by one logical unit.
        /// </summary>
        public void LineDown()
        {
            SetVerticalOffset(VerticalOffset + ItemHeight);
        }

        /// <summary>
        /// Scrolls up within content by one page.
        /// </summary>
        public void PageUp()
        {
            SetVerticalOffset(VerticalOffset - _viewport.Height);
        }

        /// <summary>
        /// Scrolls down within content by one page.
        /// </summary>
        public void PageDown()
        {
            SetVerticalOffset(VerticalOffset + _viewport.Height);
        }

        /// <summary>
        /// Scrolls up within content after a user clicks the wheel button on a mouse.
        /// </summary>
        public void MouseWheelUp()
        {
            SetVerticalOffset(VerticalOffset - ItemHeight * SystemInformation.MouseWheelScrollLines);
        }

        /// <summary>
        /// Scrolls down within content after a user clicks the wheel button on a mouse.
        /// </summary>
        public void MouseWheelDown()
        {
            SetVerticalOffset(VerticalOffset + ItemHeight * SystemInformation.MouseWheelScrollLines);
        }

        /// <summary>
        /// Scrolls left within content by one logical unit.
        /// </summary>
        /// <exception cref="System.InvalidOperationException"></exception>
        public void LineLeft()
        {
            SetHorizontalOffset(HorizontalOffset - 10);
        }

        /// <summary>
        /// Scrolls right within content by one logical unit.
        /// </summary>
        /// <exception cref="System.InvalidOperationException"></exception>
        public void LineRight()
        {
            SetHorizontalOffset(HorizontalOffset + 10);
        }

        /// <summary>
        /// Forces content to scroll until the coordinate space of a <see cref="T:System.Windows.Media.Visual" /> object is visible.
        /// </summary>
        /// <param name="visual">A <see cref="T:System.Windows.Media.Visual" /> that becomes visible.</param>
        /// <param name="rectangle">A bounding rectangle that identifies the coordinate space to make visible.</param>
        /// <returns>
        /// A <see cref="T:System.Windows.Rect" /> that is visible.
        /// </returns>
        public Rect MakeVisible(Visual visual, Rect rectangle)
        {
            return new Rect();
        }

        /// <summary>
        /// Scrolls left within content after a user clicks the wheel button on a mouse.
        /// </summary>
        /// <exception cref="System.InvalidOperationException"></exception>
        public void MouseWheelLeft()
        {
            SetHorizontalOffset(HorizontalOffset - 10 * SystemParameters.WheelScrollLines);
        }

        /// <summary>
        /// Scrolls right within content after a user clicks the wheel button on a mouse.
        /// </summary>
        /// <exception cref="System.InvalidOperationException"></exception>
        public void MouseWheelRight()
        {
            SetHorizontalOffset(HorizontalOffset + 10 * SystemParameters.WheelScrollLines);
        }

        /// <summary>
        /// Scrolls left within content by one page.
        /// </summary>
        /// <exception cref="System.InvalidOperationException"></exception>
        public void PageLeft()
        {
            SetHorizontalOffset(HorizontalOffset - _viewport.Width);
        }

        /// <summary>
        /// Scrolls right within content by one page.
        /// </summary>
        /// <exception cref="System.InvalidOperationException"></exception>
        public void PageRight()
        {
            SetHorizontalOffset(HorizontalOffset + _viewport.Width);
        }

        /// <summary>
        /// Sets the amount of horizontal offset.
        /// </summary>
        /// <param name="offset">The degree to which content is horizontally offset from the containing viewport.</param>
        /// <exception cref="System.InvalidOperationException"></exception>
        public void SetHorizontalOffset(double offset)
        {
            _offset.X = Math.Max(Math.Min(_extent.Width - _viewport.Width, offset), 0);
        }

        /// <summary>
        /// Sets the amount of vertical offset.
        /// </summary>
        /// <param name="offset">The degree to which content is vertically offset from the containing viewport.</param>
        public void SetVerticalOffset(double offset)
        {
            _offset.Y = Math.Max(Math.Min(_extent.Height - _viewport.Height, offset), 0);

            ScrollOwner?.InvalidateScrollInfo();

            // Force us to realize the correct children
            InvalidateMeasure();
        }

        private Size _extent = new Size(0, 0);
        private Size _viewport = new Size(0, 0);
        private Point _offset;

        #endregion
    }
}