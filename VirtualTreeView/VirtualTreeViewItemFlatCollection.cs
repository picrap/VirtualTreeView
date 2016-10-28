// VirtualTreeView - a TreeView that *actually* allows virtualization
// https://github.com/picrap/VirtualTreeView

namespace VirtualTreeView
{
    using System.Collections;
    using System.Windows.Controls;
    using Collection;

    /// <summary>
    /// Specialized <see cref="FlatCollection"/> for <see cref="ItemsControl.Items"/>
    /// </summary>
    public class VirtualTreeViewItemFlatCollection : FlatCollection
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="VirtualTreeViewItemFlatCollection"/> class.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="target">The target.</param>
        public VirtualTreeViewItemFlatCollection(IEnumerable source, IList target)
            : base(source, target)
        {
        }

        /// <summary>
        /// Gets a value indicating whether the item is expanded.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns></returns>
        protected override bool GetIsExpanded(object item)
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
        protected override IEnumerable GetChildren(object item)
        {
            var itemsControl = item as ItemsControl;
            return itemsControl?.Items;
        }

        /// <summary>
        /// Gets the container for item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns></returns>
        protected override object GetContainerForItem(object item)
        {
            return new VirtualTreeViewItemHolder(item);
        }

        /// <summary>
        /// Gets the item from container.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <returns></returns>
        protected virtual object GetItemFromContainer(object container)
        {
            return ((VirtualTreeViewItemHolder)container).Content;
        }
    }
}
