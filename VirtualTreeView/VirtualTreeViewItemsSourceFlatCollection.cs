// VirtualTreeView - a TreeView that *actually* allows virtualization
// https://github.com/picrap/VirtualTreeView

namespace VirtualTreeView
{
    using System.Collections;
    using Collection;

    public class VirtualTreeViewItemsSourceFlatCollection : FlatCollection
    {
        private readonly VirtualTreeView _treeView;

        public VirtualTreeViewItemsSourceFlatCollection(IList source, IList target, VirtualTreeView treeView)
            : base(source, target)
        {
            _treeView = treeView;
        }

        private VirtualTreeViewItem GetTreeViewItem(object item) => _treeView.GetContainer(item);

        protected override bool IsExpanded(object item)
        {
            return GetTreeViewItem(item)?.IsExpanded ?? false;
        }

        protected override IList GetChildren(object item)
        {
            // TODO: ensure this is correct
            return (IList)GetTreeViewItem(item)?.ItemsSource;
        }

        protected override object GetContainerForItem(object item)
        {
            return item;
        }

        protected override object GetItemFromContainer(object container)
        {
            return container;
        }
    }
}