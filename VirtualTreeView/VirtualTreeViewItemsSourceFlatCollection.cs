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
    public class VirtualTreeViewItemsSourceFlatCollection : FlatCollection
    {
        private readonly VirtualTreeView _treeView;

        /// <summary>
        /// Initializes a new instance of the <see cref="VirtualTreeViewItemsSourceFlatCollection"/> class.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="target">The target.</param>
        /// <param name="treeView">The tree view.</param>
        public VirtualTreeViewItemsSourceFlatCollection(IList source, IList target, VirtualTreeView treeView)
            : base(source, target)
        {
            _treeView = treeView;
        }

        /// <summary>
        /// Gets a value indicating whether the item is expanded.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns></returns>
        protected override bool GetIsExpanded(object item) => _treeView.IsExpanded(item);

        /// <summary>
        /// Gets the item children.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns></returns>
        protected override IList GetChildren(object item) => _treeView.GetChildren(item);

        /// <summary>
        /// Gets the container for item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns></returns>
        protected override object GetContainerForItem(object item) => item;

        /// <summary>
        /// Gets the item from container.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <returns></returns>
        protected override object GetItemFromContainer(object container) => container;
    }
}
