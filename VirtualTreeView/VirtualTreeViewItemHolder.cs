#region Arx One
// VirtualTreeView - a TreeView that *actually* allows virtualization
// https://github.com/picrap/VirtualTreeView
#endregion

namespace VirtualTreeView
{
    using System.Windows.Controls;

    /// <summary>
    /// A wrapper for <see cref="VirtualTreeViewItem"/>.
    /// This was necessary to avoid double parenthood problems
    /// </summary>
    /// <seealso cref="System.Windows.Controls.ContentControl" />
    public class VirtualTreeViewItemHolder : ContentControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="VirtualTreeViewItemHolder"/> class.
        /// </summary>
        /// <param name="item">The item.</param>
        public VirtualTreeViewItemHolder(object item)
        {
            Content = item;
        }
    }
}
