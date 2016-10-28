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
        public ObservableCollection<object> Children { get; } = new ObservableCollection<object>();

        public bool IsExpanded { get; set; }

        public string Label { get; set; }

        private static ObservableCollection<object> _root;
        public static ObservableCollection<object> Root
        {
            get
            {
                if (_root == null)
                    _root = CreateItems(2, null);
                return _root;
            }
        }

        private static ObservableCollection<object> _root2;
        public static ObservableCollection<object> Root2
        {
            get
            {
                if (_root2 == null)
                    _root2 = CreateItems(2, null);
                return _root2;
            }
        }

        public static ObservableCollection<object> CreateItems(int depth, DemoItem parent, int seed = 0, int labelIndex = 0) => CreateItems(new Random(seed), parent, 0, depth, labelIndex);

        private static ObservableCollection<object> CreateItems(Random random, DemoItem parent, int depth, int maxDepth, int labelIndex = 0)
        {
            int itemsCount = random.Next((int)Math.Pow(10, depth + 1));
            var items = new ObservableCollection<object>();
            for (int itemIndex = 0; itemIndex < itemsCount; itemIndex++)
            {
                if (random.Next(11) == 0)
                {
                    var errorItem = new ErrorItem();
                    items.Add(errorItem);
                }
                else
                {
                    var item = new DemoItem();
                    item.Label = (parent != null ? parent.Label + "." : "") + ++labelIndex;
                    item.IsExpanded = random.Next(5) == 0;
                    items.Add(item);
                    if (depth < maxDepth)
                    {
                        foreach (var childItem in CreateItems(random, item, depth + 1, maxDepth))
                            item.Children.Add(childItem);
                    }
                }
            }
            return items;
        }
    }
}