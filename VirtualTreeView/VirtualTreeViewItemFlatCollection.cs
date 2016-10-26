// VirtualTreeView - a TreeView that *actually* allows virtualization
// https://github.com/picrap/VirtualTreeView

namespace VirtualTreeView
{
    using System.Collections;
    using System.Windows.Controls;
    using Collection;

    public class VirtualTreeViewItemFlatCollection : FlatCollection
    {
        public VirtualTreeViewItemFlatCollection(IList source, IList target)
            : base(source, target)
        {
        }

        protected override bool GetIsExpanded(object item)
        {
            var virtualTreeViewItem = item as VirtualTreeViewItem;
            if (virtualTreeViewItem != null)
                return virtualTreeViewItem.IsExpanded;
            return false;
        }

        protected override IList GetChildren(object item)
        {
            var itemsControl = item as ItemsControl;
            return itemsControl?.Items;
        }

        protected override object GetContainerForItem(object item)
        {
            return new VirtualTreeViewItemHolder(item);
        }

        protected override object GetItemFromContainer(object container)
        {
            return ((VirtualTreeViewItemHolder)container).Content;
        }
    }
}
