// VirtualTreeView - a TreeView that *actually* allows virtualization
// https://github.com/picrap/VirtualTreeView

namespace VirtualTreeView
{
    using System.Collections;
    using System.Windows.Controls;
    using Collection;

    /// <summary>
    /// Specialized <see cref="FlatCollection"/> for <see cref="ItemsControl.ItemsSource"/>.
    /// </summary>
    public class ItemsSourceHierarchicalSource : IHierarchicalSource
    {
        private readonly VirtualTreeView _treeView;

        /// <summary>
        /// Gets the source.
        /// </summary>
        /// <value>
        /// The source.
        /// </value>
        public IEnumerable Source { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ItemsSourceHierarchicalSource"/> class.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="treeView">The tree view.</param>
        public ItemsSourceHierarchicalSource(IEnumerable source, VirtualTreeView treeView)
        {
            _treeView = treeView;
            Source = source;
        }

        /// <summary>
        /// Gets a value indicating whether the item is expanded.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns></returns>
        public bool IsExpanded(object item) => _treeView.IsExpanded(item);

        /// <summary>
        /// Gets the item children.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns></returns>
        public IEnumerable GetChildren(object item) => _treeView.GetChildren(item);

        /// <summary>
        /// Gets the container for item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns></returns>
        public object GetContainerForItem(object item) => item;
    }
}
