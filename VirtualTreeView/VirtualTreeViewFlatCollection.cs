// VirtualTreeView - a TreeView that *actually* allows virtualization
// https://github.com/picrap/VirtualTreeView

namespace VirtualTreeView
{
    using System.Collections;
    using System.Windows.Controls;
    using Collection;

    public class VirtualTreeViewFlatCollection : FlatCollection
    {
        public VirtualTreeViewFlatCollection(IList source, IList target)
            : base(source, target)
        {
        }

        protected override bool IsExpanded(object item)
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

        protected override object GenerateItemHolder(object item)
        {
            return new VirtualTreeViewItemHolder(item);
        }

        protected override object GetHeldItem(object item)
        {
            return ((VirtualTreeViewItemHolder)item).Content;
        }
    }
}
