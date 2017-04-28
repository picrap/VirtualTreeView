// VirtualTreeView - a TreeView that *actually* allows virtualization
// https://github.com/picrap/VirtualTreeView

namespace VirtualTreeView
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using System.Windows.Forms;
    using System.Windows.Media;

    /// <summary>
    /// Optimized <see cref="VirtualizingPanel"/> for rows
    /// Inspired from https://blogs.msdn.microsoft.com/dancre/2006/02/17/implementing-a-virtualizingpanel-part-4-the-goods/
    /// </summary>
    /// <seealso cref="System.Windows.Controls.VirtualizingPanel" />
    public class RowsVirtualizingPanel : VirtualizingPanel, IScrollInfo
    {
        private readonly TranslateTransform _translateTransform = new TranslateTransform();

        /// <summary>
        /// Initializes a new instance of the <see cref="RowsVirtualizingPanel"/> class.
        /// </summary>
        public RowsVirtualizingPanel()
        {
            // For use in the IScrollInfo implementation
            //this.RenderTransform = _translateTransform;
        }

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

        private IItemContainerGenerator GetItemContainerGenerator()
        {
            var children = this.InternalChildren;
            var generator = ItemContainerGenerator;
            return generator;
        }

        /// <summary>
        /// Measure the children
        /// </summary>
        /// <param name="availableSize">Size available</param>
        /// <returns>Size desired</returns>
        protected Size MeasureOverride1(Size availableSize)
        {
            UpdateScrollInfo(availableSize);

            if (double.IsInfinity(ItemHeight))
                return new Size(ActualWidth, ActualHeight);

            // Figure out range that's visible based on layout algorithm
            int firstVisibleItemIndex, lastVisibleItemIndex;
            GetVisibleRange(out firstVisibleItemIndex, out lastVisibleItemIndex);

            // We need to access InternalChildren before the generator to work around a bug
            var children = this.InternalChildren;
            var generator = GetItemContainerGenerator();

            // Get the generator position of the first visible data item
            var startPos = generator.GeneratorPositionFromIndex(firstVisibleItemIndex);

            // Get index where we'd insert the child for this position. If the item is realized
            // (position.Offset == 0), it's just position.Index, otherwise we have to add one to
            // insert after the corresponding child
            int childIndex = startPos.Offset == 0 ? startPos.Index : startPos.Index + 1;

            var itemHeight = ItemHeight;
            using (generator.StartAt(startPos, GeneratorDirection.Forward, true))
            {
                for (int itemIndex = firstVisibleItemIndex; itemIndex <= lastVisibleItemIndex; ++itemIndex, ++childIndex)
                {
                    bool newlyRealized;

                    // Get or create the child
                    var child = generator.GenerateNext(out newlyRealized) as UIElement;
                    if (newlyRealized)
                    {
                        // Figure out if we need to insert the child at the end or somewhere in the middle
                        if (childIndex >= children.Count)
                        {
                            base.AddInternalChild(child);
                        }
                        else
                        {
                            base.InsertInternalChild(childIndex, child);
                        }
                        generator.PrepareItemContainer(child);
                    }
                    else if (child != null)
                    {
                        // The child has already been created, let's be sure it's in the right spot
                        Debug.Assert(child == children[childIndex], "Wrong child was generated");
                    }

                    // Measurements will depend on layout algorithm
                    if (child != null)
                        child.Measure(new Size(double.PositiveInfinity, itemHeight));
                }
            }

            // Note: this could be deferred to idle time for efficiency
            CleanUpItems(firstVisibleItemIndex, lastVisibleItemIndex);

            var itemCount = ItemCount;

            //return availableSize;
            return new Size(ActualWidth, ItemHeight * itemCount);
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

        /// <summary>
        /// Arrange the children
        /// </summary>
        /// <param name="finalSize">Size available</param>
        /// <returns>Size used</returns>
        protected Size ArrangeOverride1(Size finalSize)
        {
            IItemContainerGenerator generator = GetItemContainerGenerator();

            UpdateScrollInfo(finalSize);

            for (int i = 0; i < this.Children.Count; i++)
            {
                UIElement child = this.Children[i];

                // Map the child offset to an item offset
                int itemIndex = generator.IndexFromGeneratorPosition(new GeneratorPosition(i, 0));

                ArrangeChild(itemIndex, child, finalSize);
            }

            return finalSize;
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
            UpdateScrollInfo(availableSize);
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
            return new Size(width, ItemCount * itemHeight);
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
                if (InternalChildren.Contains(child))
                    remainingChildren.Remove(child);
                else
                    AddInternalChild(child);
                ArrangeChild(index, child, finalSize);
            }
            foreach (var remainingChild in remainingChildren)
                RemoveInternalChildRange(InternalChildren.IndexOf(remainingChild), 1);
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
        /// Revirtualize items that are no longer visible
        /// </summary>
        /// <param name="minDesiredGenerated">first item index that should be visible</param>
        /// <param name="maxDesiredGenerated">last item index that should be visible</param>
        private void CleanUpItems(int minDesiredGenerated, int maxDesiredGenerated)
        {
            UIElementCollection children = this.InternalChildren;
            IItemContainerGenerator generator = GetItemContainerGenerator();

            for (int i = children.Count - 1; i >= 0; i--)
            {
                GeneratorPosition childGeneratorPos = new GeneratorPosition(i, 0);
                int itemIndex = generator.IndexFromGeneratorPosition(childGeneratorPos);
                if (itemIndex < minDesiredGenerated || itemIndex > maxDesiredGenerated)
                {
                    generator.Remove(childGeneratorPos, 1);
                    RemoveInternalChildRange(i, 1);
                }
            }
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

        #region Layout specific code

        // I've isolated the layout specific code to this region. If you want to do something other than tiling, this is
        // where you'll make your changes

        /// <summary>
        /// Calculate the extent of the view based on the available size
        /// </summary>
        /// <param name="availableSize">available size</param>
        /// <param name="itemCount">number of data items</param>
        /// <returns></returns>
        private Size CalculateExtent(Size availableSize, int itemCount)
        {
            var width = availableSize.Width; // double.PositiveInfinity;
            if (itemCount == 0 || !ValidItemHeight)
                return new Size(width, 0);
            // See how big we are
            return new Size(width, ItemHeight * Math.Ceiling((double) itemCount));
        }

        /// <summary>
        /// Get the range of children that are visible
        /// </summary>
        /// <param name="firstVisibleItemIndex">The item index of the first visible item</param>
        /// <param name="lastVisibleItemIndex">The item index of the last visible item</param>
        private void GetVisibleRange(out int firstVisibleItemIndex, out int lastVisibleItemIndex)
        {
            var itemsControl = ItemsControl.GetItemsOwner(this);
            int itemCount = itemsControl.HasItems ? itemsControl.Items.Count : 0;

            if (!ValidItemHeight)
            {
                firstVisibleItemIndex = 0;
                lastVisibleItemIndex = itemCount;
                return;
            }

            var itemHeight = ItemHeight;
            firstVisibleItemIndex = (int) Math.Floor(_offset.Y / itemHeight);
            //    lastVisibleItemIndex = (int) Math.Ceiling((_offset.Y + _viewport.Height) / itemHeight) - 1;
            lastVisibleItemIndex = (int) Math.Ceiling((_offset.Y + ActualHeight) / itemHeight) + 10;

            if (lastVisibleItemIndex >= itemCount)
                lastVisibleItemIndex = itemCount - 1;
        }

        /// <summary>
        /// Position a child
        /// </summary>
        /// <param name="itemIndex">The data item index of the child</param>
        /// <param name="child">The element to position</param>
        /// <param name="finalSize">The size of the panel</param>
        private void ArrangeChild(int itemIndex, UIElement child, Size finalSize)
        {
            var lineOffset = _offset.Y % ItemHeight;
            child.Arrange(new Rect(-_offset.X, itemIndex * ItemHeight - lineOffset, finalSize.Width, ItemHeight));
        }

        #endregion

        #region IScrollInfo implementation

        // See Ben Constable's series of posts at http://blogs.msdn.com/bencon/


        private void UpdateScrollInfo(Size availableSize)
        {
            // See how many items there are
            ItemsControl itemsControl = ItemsControl.GetItemsOwner(this);
            int itemCount = itemsControl.HasItems ? itemsControl.Items.Count : 0;

            Size extent = CalculateExtent(availableSize, itemCount);
            // Update extent
            if (extent != _extent)
            {
                _extent = extent;
                ScrollOwner?.InvalidateScrollInfo();
            }

            // Update viewport
            if (availableSize != _viewport)
            {
                _viewport = availableSize;
                ScrollOwner?.InvalidateScrollInfo();
            }
        }

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

            _translateTransform.Y = -offset;

            // Force us to realize the correct children
            InvalidateMeasure();
        }

        private Size _extent = new Size(0, 0);
        private Size _viewport = new Size(0, 0);
        private Point _offset;

        #endregion
    }
}