// VirtualTreeView - a TreeView that *actually* allows virtualization
// https://github.com/picrap/VirtualTreeView

namespace VirtualTreeViewDemo
{
    using System;
    using System.Collections.ObjectModel;
    using System.Diagnostics;

    [DebuggerDisplay("{Label}")]
    public class DemoItem
    {
        public ObservableCollection<DemoItem> Children { get; } = new ObservableCollection<DemoItem>();

        public bool IsExpanded { get; set; }

        public string Label { get; set; }

        private static DemoItem[] _root;
        public static DemoItem[] Root
        {
            get
            {
                if (_root == null)
                    _root = CreateItems(2, null);
                return _root;
            }
        }

        private static DemoItem[] _root2;
        public static DemoItem[] Root2
        {
            get
            {
                if (_root2 == null)
                    _root2 = CreateItems(2, null);
                return _root2;
            }
        }

        private static DemoItem[] CreateItems(int depth, DemoItem parent, int seed = 0) => CreateItems(new Random(seed), parent, 0, depth);

        private static DemoItem[] CreateItems(Random random, DemoItem parent, int depth, int maxDepth)
        {
            int itemsCount = random.Next((int)Math.Pow(10, depth + 1));
            var items = new DemoItem[itemsCount];
            for (int itemIndex = 0; itemIndex < itemsCount; itemIndex++)
            {
                var item = new DemoItem();
                item.Label = (parent != null ? parent.Label + "." : "") + (itemIndex + 1);
                item.IsExpanded = random.Next(5) == 0;
                items[itemIndex] = item;
                if (depth < maxDepth)
                {
                    foreach (var childItem in CreateItems(random, item, depth + 1, maxDepth))
                        item.Children.Add(childItem);
                }
            }
            return items;
        }
    }
}