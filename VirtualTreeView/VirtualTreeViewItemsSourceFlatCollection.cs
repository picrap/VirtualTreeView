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

        protected override bool IsExpanded(object item)
        {
            return _treeView.IsExpanded(item);
        }

        protected override IList GetChildren(object item)
        {
            return _treeView.GetChildren(item);
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
