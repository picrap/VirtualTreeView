// VirtualTreeView - a TreeView that *actually* allows virtualization
// https://github.com/picrap/VirtualTreeView

namespace VirtualTreeView
{
    using System;
    using System.Collections.Specialized;
    using System.Diagnostics;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using System.Windows.Media;

    /// <summary>
    /// Optimized <see cref="VirtualizingPanel"/> for rows
    /// Inspired from https://blogs.msdn.microsoft.com/dancre/2006/02/17/implementing-a-virtualizingpanel-part-4-the-goods/
    /// </summary>
    /// <seealso cref="System.Windows.Controls.VirtualizingPanel" />
    class RowsVirtualizingPanel : VirtualizingPanel, IScrollInfo
    {
        public RowsVirtualizingPanel()
        {
            // For use in the IScrollInfo implementation
            this.RenderTransform = _trans;
        }

        private double? _itemHeight;

        private double ItemHeight
        {
            get
            {
                var itemHeight = _itemHeight ?? 0;
                if (itemHeight == 0 || double.IsPositiveInfinity(itemHeight))
                {
                    var generator = GetItemContainerGenerator();
                    // Get the generator position of the first visible data item
                    var startPos = generator.GeneratorPositionFromIndex(0);
                    bool newlyRealized;
                    using (generator.StartAt(startPos, GeneratorDirection.Forward, true))
                    {
                        var child = generator.GenerateNext(out newlyRealized) as UIElement;
                        if (child != null)
                        {
                            child.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                            _itemHeight = child.DesiredSize.Height;
                        }
                        else
                            _itemHeight = double.PositiveInfinity;
                        //generator.Remove(new GeneratorPosition(i, 0), 1);
                    }
                    if (newlyRealized)
                        generator.RemoveAll();
                }
                return _itemHeight.Value;
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
        protected override Size MeasureOverride(Size availableSize)
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

            var itemsControl = ItemsControl.GetItemsOwner(this);
            int itemCount = itemsControl.HasItems ? itemsControl.Items.Count : 0;

            //return availableSize;
            return new Size(ActualWidth, ItemHeight * itemCount);
        }

        /// <summary>
        /// Arrange the children
        /// </summary>
        /// <param name="finalSize">Size available</param>
        /// <returns>Size used</returns>
        protected override Size ArrangeOverride(Size finalSize)
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
            switch (args.Action)
            {
                case NotifyCollectionChangedAction.Remove:
                case NotifyCollectionChangedAction.Replace:
                case NotifyCollectionChangedAction.Move:
                    RemoveInternalChildRange(args.Position.Index, args.ItemUICount);
                    break;
            }
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
            var width = double.PositiveInfinity;
            if (itemCount == 0)
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
            var itemHeight = ItemHeight;

            var itemsControl = ItemsControl.GetItemsOwner(this);
            int itemCount = itemsControl.HasItems ? itemsControl.Items.Count : 0;

            if (itemHeight == 0 || double.IsInfinity(itemHeight))
            {
                firstVisibleItemIndex = 0;
                lastVisibleItemIndex = itemCount;
                return;
            }

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
            int row = itemIndex;
            child.Arrange(new Rect(0, row * ItemHeight, finalSize.Width, ItemHeight));
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
                if (_owner != null)
                    _owner.InvalidateScrollInfo();
            }

            // Update viewport
            if (availableSize != _viewport)
            {
                _viewport = availableSize;
                if (_owner != null)
                    _owner.InvalidateScrollInfo();
            }
        }

        public ScrollViewer ScrollOwner
        {
            get { return _owner; }
            set { _owner = value; }
        }

        public bool CanHorizontallyScroll
        {
            get { return _canHScroll; }
            set { _canHScroll = value; }
        }

        public bool CanVerticallyScroll
        {
            get { return _canVScroll; }
            set { _canVScroll = value; }
        }

        public double HorizontalOffset
        {
            get { return _offset.X; }
        }

        public double VerticalOffset
        {
            get { return _offset.Y; }
        }

        public double ExtentHeight
        {
            get { return _extent.Height; }
        }

        public double ExtentWidth
        {
            get { return _extent.Width; }
        }

        public double ViewportHeight
        {
            get { return _viewport.Height; }
        }

        public double ViewportWidth
        {
            get { return _viewport.Width; }
        }

        public void LineUp()
        {
            SetVerticalOffset(this.VerticalOffset - 10);
        }

        public void LineDown()
        {
            SetVerticalOffset(this.VerticalOffset + 10);
        }

        public void PageUp()
        {
            SetVerticalOffset(this.VerticalOffset - _viewport.Height);
        }

        public void PageDown()
        {
            SetVerticalOffset(this.VerticalOffset + _viewport.Height);
        }

        public void MouseWheelUp()
        {
            SetVerticalOffset(this.VerticalOffset - 10);
        }

        public void MouseWheelDown()
        {
            SetVerticalOffset(this.VerticalOffset + 10);
        }

        public void LineLeft()
        {
            throw new InvalidOperationException();
        }

        public void LineRight()
        {
            throw new InvalidOperationException();
        }

        public Rect MakeVisible(Visual visual, Rect rectangle)
        {
            return new Rect();
        }

        public void MouseWheelLeft()
        {
            throw new InvalidOperationException();
        }

        public void MouseWheelRight()
        {
            throw new InvalidOperationException();
        }

        public void PageLeft()
        {
            throw new InvalidOperationException();
        }

        public void PageRight()
        {
            throw new InvalidOperationException();
        }

        public void SetHorizontalOffset(double offset)
        {
            throw new InvalidOperationException();
        }

        public void SetVerticalOffset(double offset)
        {
            if (offset < 0 || _viewport.Height >= _extent.Height)
            {
                offset = 0;
            }
            else
            {
                if (offset + _viewport.Height >= _extent.Height)
                {
                    offset = _extent.Height - _viewport.Height;
                }
            }

            _offset.Y = offset;

            if (_owner != null)
                _owner.InvalidateScrollInfo();

            _trans.Y = -offset;

            // Force us to realize the correct children
            InvalidateMeasure();
        }

        private TranslateTransform _trans = new TranslateTransform();
        private ScrollViewer _owner;
        private bool _canHScroll = false;
        private bool _canVScroll = false;
        private Size _extent = new Size(0, 0);
        private Size _viewport = new Size(0, 0);
        private Point _offset;

        #endregion
    }
}