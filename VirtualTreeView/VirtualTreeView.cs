// VirtualTreeView - a TreeView that *actually* allows virtualization
// https://github.com/picrap/VirtualTreeView

namespace VirtualTreeView
{
    using System.Collections.Specialized;
    using System.Windows;
    using System.Windows.Controls;
    using System.Linq;
    using System.Windows.Media;

    [StyleTypedProperty(Property = "ItemContainerStyle", StyleTargetType = typeof(TreeViewItem))]
    public class VirtualTreeView : ItemsControl
    {
        static VirtualTreeView()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(VirtualTreeView), new FrameworkPropertyMetadata(typeof(ItemsControl)));
        }

        public VirtualTreeView()
        {
            INotifyCollectionChanged notifyCollectionChangedItems = Items;
            notifyCollectionChangedItems.CollectionChanged += OnItemsCollectionChanged;
        }

        private void OnItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
        }
    }
}
