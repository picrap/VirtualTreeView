// VirtualTreeView - a TreeView that *actually* allows virtualization
// https://github.com/picrap/VirtualTreeView

namespace VirtualTreeView.Collection
{
    using System.Collections;

    /// <summary>
    /// Represents a hierarchical source
    /// </summary>
    public interface IHierarchicalSource
    {
        /// <summary>
        /// Gets the source.
        /// </summary>
        /// <value>
        /// The source.
        /// </value>
        IEnumerable Source { get; }

        /// <summary>
        /// Gets a value indicating whether the item is expanded.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns></returns>
        bool GetIsExpanded(object item);

        /// <summary>
        /// Gets the item children.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns></returns>
        IEnumerable GetChildren(object item);

        /// <summary>
        /// Gets the container for item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns></returns>
        object GetContainerForItem(object item);
    }
}
