// VirtualTreeView - a TreeView that *actually* allows virtualization
// https://github.com/picrap/VirtualTreeView

namespace VirtualTreeViewDemo
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Interop;
    using System.Windows.Media;
    using VirtualTreeView;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            ComponentDispatcher.ThreadIdle += OnIdle;
        }

        private DateTime _lastUpdate;

        private void OnIdle(object sender, EventArgs e)
        {
            var now = DateTime.Now;
            if ((now - _lastUpdate).TotalSeconds < 2)
                return;

            _lastUpdate = now;

            var treeViewCount = GetDescendants(TreeView).OfType<TreeViewItem>().Count();
            TreeViewCount.Text = $"{treeViewCount} items";
            var virtualTreeViewCount = GetDescendants(VirtualTreeView).OfType<VirtualTreeViewItem>().Count();
            VirtualTreeViewCount.Text = $"{virtualTreeViewCount} items";
        }

        private IEnumerable<DependencyObject> GetChildren(DependencyObject d)
        {
            var count = VisualTreeHelper.GetChildrenCount(d);
            for (int i = 0; i < count; i++)
                yield return VisualTreeHelper.GetChild(d, i);
        }

        private IEnumerable<DependencyObject> GetDescendants(DependencyObject d)
        {
            return GetChildren(d).SelectMany(c => new[] { c }.Concat(GetDescendants(c)));
        }

        private void AppendDemoItems(object sender, RoutedEventArgs e)
        {
            foreach (var i in DemoItem.CreateItems(2, null, DemoItem.Root.Count))
                DemoItem.Root.Add(i);
            foreach (var i in DemoItem.CreateItems(2, null, DemoItem.Root2.Count))
                DemoItem.Root2.Add(i);
        }
    }
}