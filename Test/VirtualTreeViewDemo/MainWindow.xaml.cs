// VirtualTreeView - a TreeView that *actually* allows virtualization
// https://github.com/picrap/VirtualTreeView

namespace VirtualTreeViewDemo
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
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
            AppendDemoItems(DemoItem.Root);
            AppendDemoItems(DemoItem.Root2);
        }

        private static void AppendDemoItems(ObservableCollection<object> root)
        {
            foreach (var i in DemoItem.CreateItems(2, null, root.Count))
                root.Add(i);
        }

        private void ReplaceFirstContent(object sender, RoutedEventArgs e)
        {
            ReplaceFirstContent(DemoItem.Root);
            ReplaceFirstContent(DemoItem.Root2);
        }

        private static void ReplaceFirstContent(ObservableCollection<object> root)
        {
            var firstItem = root[0] as DemoItem;
            if (firstItem == null)
                root[0] = firstItem = new DemoItem { Label = "1" };
            firstItem.Children.Clear();
            foreach (var childItem in DemoItem.CreateItems(1, firstItem, seed: 3))
                firstItem.Children.Add(childItem);
        }

        private void AppendFirstContent(object sender, RoutedEventArgs e)
        {
            AppendFirstContent(DemoItem.Root);
            AppendFirstContent(DemoItem.Root2);
        }

        private static void AppendFirstContent(ObservableCollection<object> root)
        {
            var firstItem = root[0] as DemoItem;
            if (firstItem == null)
                root[0] = firstItem = new DemoItem { Label = "1" };
            foreach (var childItem in DemoItem.CreateItems(1, firstItem, seed: 3, labelIndex: firstItem.Children.Count))
                firstItem.Children.Add(childItem);
        }
    }
}