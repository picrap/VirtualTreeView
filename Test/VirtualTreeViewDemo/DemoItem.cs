// VirtualTreeView - a TreeView that *actually* allows virtualization
// https://github.com/picrap/VirtualTreeView

namespace VirtualTreeViewDemo
{
    using System;
    using System.Collections.ObjectModel;

    public class DemoItem
    {
        public ObservableCollection<DemoItem> Children { get; } = new ObservableCollection<DemoItem>();

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

        private static DemoItem[] CreateItems(int depth, DemoItem parent, int seed = 0) => CreateItems(new Random(seed), parent, depth);

        private static DemoItem[] CreateItems(Random random, DemoItem parent, int depth)
        {
            int itemsCount = random.Next(10);
            var items = new DemoItem[itemsCount];
            for (int itemIndex = 0; itemIndex < itemsCount; itemIndex++)
            {
                var item = new DemoItem();
                item.Label = (parent != null ? parent.Label + "." : "") + (itemIndex + 1);
                items[itemIndex] = item;
                if (depth > 0)
                {
                    foreach (var childItem in CreateItems(random, item, depth - 1))
                        item.Children.Add(childItem);
                }
            }
            return items;
        }
    }
}