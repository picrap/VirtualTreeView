#region Arx One
// VirtualTreeView - a TreeView that *actually* allows virtualization
// https://github.com/picrap/VirtualTreeView
#endregion

namespace VirtualTreeView
{
    using System.Windows.Controls;

    public class VirtualTreeViewItemHolder : ContentControl
    {
        public VirtualTreeViewItemHolder(object item)
        {
            Content = item;
        }
    }
}
