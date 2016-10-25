// VirtualTreeView - a TreeView that *actually* allows virtualization
// https://github.com/picrap/VirtualTreeView

namespace VirtualTreeView
{
    using System.ComponentModel;
    using System.Runtime.CompilerServices;
    using System.Windows;
    using System.Windows.Controls;

    public class VirtualTreeViewItem : HeaderedItemsControl
    {
        public static readonly DependencyProperty IsExpandedProperty = DependencyProperty.Register(
            "IsExpanded", typeof(bool), typeof(VirtualTreeViewItem), new PropertyMetadata(default(bool)));

        public bool IsExpanded
        {
            get { return (bool) GetValue(IsExpandedProperty); }
            set { SetValue(IsExpandedProperty, value); }
        }

        public static readonly DependencyProperty IsSelectedProperty = DependencyProperty.Register(
            "IsSelected", typeof(bool), typeof(VirtualTreeViewItem), new PropertyMetadata(default(bool)));

        public bool IsSelected
        {
            get { return (bool) GetValue(IsSelectedProperty); }
            set { SetValue(IsSelectedProperty, value); }
        }

        public static readonly DependencyProperty IsSelectionActiveProperty = DependencyProperty.Register(
            "IsSelectionActive", typeof(bool), typeof(VirtualTreeViewItem), new PropertyMetadata(default(bool)));

        public bool IsSelectionActive
        {
            get { return (bool) GetValue(IsSelectionActiveProperty); }
            set { SetValue(IsSelectionActiveProperty, value); }
        }

        static VirtualTreeViewItem()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(VirtualTreeViewItem), new FrameworkPropertyMetadata(typeof(VirtualTreeViewItem)));
        }
    }
}
