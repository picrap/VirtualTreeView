// VirtualTreeView - a TreeView that *actually* allows virtualization
// https://github.com/picrap/VirtualTreeView

namespace VirtualTreeView
{
    using System;
    using System.Collections;
    using System.Windows.Controls;
    using Collection;

    /// <summary>
    /// Specialized <see cref="FlatCollection"/> for <see cref="ItemsControl.Items"/>
    /// </summary>
    public class ItemHierarchicalSource : IHierarchicalSource
    {
        /// <summary>
        /// Gets the source.
        /// </summary>
        /// <value>
        /// The source.
        /// </value>
        public IEnumerable Source { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ItemHierarchicalSource" /> class.
        /// </summary>
        /// <param name="source">The source.</param>
        public ItemHierarchicalSource(IEnumerable source)
        {
            Source = source;
        }

        /// <summary>
        /// Gets a value indicating whether the item is expanded.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns></returns>
        public bool IsExpanded(object item)
        {
            var virtualTreeViewItem = item as VirtualTreeViewItem;
            if (virtualTreeViewItem != null)
                return virtualTreeViewItem.IsExpanded;
            return false;
        }

        /// <summary>
        /// Gets the item children.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns></returns>
        public IEnumerable GetChildren(object item)
        {
            var itemsControl = item as ItemsControl;
            return itemsControl?.Items;
        }

        /// <summary>
        /// Gets the container for item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns></returns>
        public object GetContainerForItem(object item)
        {
            return new VirtualTreeViewItemHolder(item);
        }
    }
}
