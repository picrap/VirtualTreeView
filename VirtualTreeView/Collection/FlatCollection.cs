// VirtualTreeView - a TreeView that *actually* allows virtualization
// https://github.com/picrap/VirtualTreeView

namespace VirtualTreeView.Collection
{
    using System;
    using System.Collections;
    using System.Collections.Specialized;
    using Reflection;

    public abstract class FlatCollection
    {
        private readonly IList _target;

        public FlatCollection(IList source, IList target)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (target == null)
                throw new ArgumentNullException(nameof(target));
            _target = target;
            source.IfType<INotifyCollectionChanged>(nc => nc.CollectionChanged += (o, args) => OnCollectionChanged(null, args));
            InsertItems(0, source);
        }

        private void Clear() => _target.Clear();

        protected abstract bool IsExpanded(object item);
        protected abstract IList GetChildren(object item);

        protected abstract object GenerateItemHolder(object item);
        protected abstract object GetHeldItem(object item);

        public void Expand(object item)
        {
            var itemChildren = GetChildren(item);
            if (itemChildren != null && itemChildren.Count > 0)
            {
                var itemIndex = GetItemIndex(item);
                InsertItems(itemIndex + 1, itemChildren);
            }
        }

        public void Collapse(object item)
        {
            var itemIndex = GetItemIndex(item);
            var lastChildIndex = GetLastChildIndex(item, false);
            DeleteItems(itemIndex + 1, lastChildIndex - itemIndex);
        }

        private void AppendItem(object item) => InsertItem(_target.Count, item);
        private void AppendItems(IList items) => InsertItems(_target.Count, items);

        private int InsertItem(int index, object item)
        {
            var count = 1;
            _target.Insert(index, GenerateItemHolder(item));
            var itemChildren = GetChildren(item);
            if (IsExpanded(item) && itemChildren != null)
                count += InsertItems(index + 1, itemChildren);
            itemChildren.IfType<INotifyCollectionChanged>(c => c.CollectionChanged += (sender, args) => OnCollectionChanged(item, args));
            return count;
        }

        private void OnCollectionChanged(object parent, NotifyCollectionChangedEventArgs e)
        {
            if (parent != null && !IsExpanded(parent))
                return;
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    InsertItems(GetInsertIndex(parent, e.NewStartingIndex), e.NewItems);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    throw new NotImplementedException();
                    break;
                case NotifyCollectionChangedAction.Replace:
                    throw new NotImplementedException();
                    break;
                case NotifyCollectionChangedAction.Move:
                    throw new NotImplementedException();
                    break;
                case NotifyCollectionChangedAction.Reset:
                    if (parent == null)
                        Clear();
                    else
                        Collapse(parent);
                    var children = GetChildren(parent);
                    if (children != null)
                        InsertItems(GetItemIndex(parent) + 1, children);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public int InsertItems(int index, IList items)
        {
            var startIndex = index;
            foreach (var i in items)
                index += InsertItem(index, i);
            return index - startIndex;
        }

        public void DeleteItems(int index, int count)
        {
            while (count-- > 0)
                _target.RemoveAt(index);
        }

        /// <summary>
        /// Gets the index where an item can be insertedn given the parent and child index.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="childIndex">Index of the child.</param>
        /// <returns></returns>
        private int GetInsertIndex(object item, int childIndex)
        {
            if (item == null) // root item
            {
                // first is always first
                if (childIndex == 0)
                    return 0;
                // next follows the previous item and its children
                return GetLastChildIndex(GetHeldItem(_target[childIndex - 1]), true) + 1;
            }
            return GetLastChildIndex(GetChildren(item)[childIndex], true);
        }

        /// <summary>
        /// Gets the last child index.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="onlyVisible">if set to <c>true</c> [only visible].</param>
        /// <returns></returns>
        public int GetLastChildIndex(object item, bool onlyVisible)
        {
            var itemChildren = GetChildren(item);
            if (itemChildren == null || itemChildren.Count == 0 || (onlyVisible && !IsExpanded(item)))
                return GetItemIndex(item);

            return GetLastChildIndex(itemChildren[itemChildren.Count - 1], onlyVisible);
        }

        /// <summary>
        /// Gets the index of the item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns></returns>
        public int GetItemIndex(object item)
        {
            // root collection? no index
            if (item == null)
                return -1;
            for (int index = 0; index < _target.Count; index++)
            {
                var indexedItem = GetHeldItem(_target[index]);
                if (ReferenceEquals(indexedItem, item))
                    return index;
            }
            return -1;
        }
    }
}