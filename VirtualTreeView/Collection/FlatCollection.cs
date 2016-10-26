// VirtualTreeView - a TreeView that *actually* allows virtualization
// https://github.com/picrap/VirtualTreeView

namespace VirtualTreeView.Collection
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
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
            if (target.Count > 0)
                throw new ArgumentException(@"Must be empty", nameof(target));
            _target = target;
            source.IfType<INotifyCollectionChanged>(nc => nc.CollectionChanged += (o, args) => OnCollectionChanged(null, source, args));
            InsertItems(0, source, null);
        }

        private void Clear()
        {
            _target.Clear();
            _parentsByItems.Clear();
        }

        protected abstract bool IsExpanded(object item);
        protected abstract IList GetChildren(object item);

        protected abstract object GetContainerForItem(object item);
        protected abstract object GetItemFromContainer(object container);

        public void Expand(object item)
        {
            var itemChildren = GetChildren(item);
            if (itemChildren != null && itemChildren.Count > 0)
            {
                var itemIndex = GetItemIndex(item);
                InsertItems(itemIndex + 1, itemChildren, item);
            }
        }

        public void Collapse(object item)
        {
            var itemIndex = GetItemIndex(item);
            var lastChildIndex = GetLastChildIndex(item, false);
            DeleteItems(itemIndex + 1, lastChildIndex - itemIndex);
        }

        private readonly IDictionary<object, object> _parentsByItems = new Dictionary<object, object>();

        public object GetParent(object item)
        {
            object parent;
            _parentsByItems.TryGetValue(item, out parent);
            return parent;
        }

        private int InsertItem(int index, object item, object parent)
        {
            var count = 1;
            _target.Insert(index, GetContainerForItem(item));
            _parentsByItems[item] = parent;
            var itemChildren = GetChildren(item);
            if (IsExpanded(item) && itemChildren != null)
                count += InsertItems(index + 1, itemChildren, item);
            itemChildren.IfType<INotifyCollectionChanged>(c => c.CollectionChanged += (sender, args) => OnCollectionChanged(item, itemChildren, args));
            return count;
        }

        private void OnCollectionChanged(object parent, IList collection, NotifyCollectionChangedEventArgs e)
        {
            if (parent != null && !IsExpanded(parent))
                return;
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    InsertItems(GetInsertIndex(collection, e.NewStartingIndex), e.NewItems, parent);
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
                        InsertItems(GetItemIndex(parent) + 1, children, parent);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private int InsertItems(int index, IEnumerable items, object parent)
        {
            var startIndex = index;
            foreach (var i in items)
                index += InsertItem(index, i, parent);
            return index - startIndex;
        }

        public void DeleteItems(int index, int count)
        {
            while (count-- > 0)
            {
                _parentsByItems.Remove(_target[index]);
                _target.RemoveAt(index);
            }
        }

        /// <summary>
        /// Gets the index where an item can be insertedn given the parent and child index.
        /// </summary>
        /// <param name="collection">The collection.</param>
        /// <param name="childIndex">Index of the child.</param>
        /// <returns></returns>
        private int GetInsertIndex(IList collection, int childIndex)
        {
            //if (item == null) // root item
            //{
            //    // first is always first
            //    if (childIndex == 0)
            //        return 0;
            //    // next follows the previous item and its children
            //    return GetLastChildIndex(GetItemFromContainer(_target[childIndex - 1]), true) + 1;
            //}
            if (childIndex == 0)
                return 0;
            return GetLastChildIndex(collection[childIndex - 1], true) + 1;
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

            return GetLastChildIndex(itemChildren[itemChildren.Count - 1], true);
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
                var indexedItem = GetItemFromContainer(_target[index]);
                if (ReferenceEquals(indexedItem, item))
                    return index;
            }
            return -1;
        }
    }
}