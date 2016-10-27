// VirtualTreeView - a TreeView that *actually* allows virtualization
// https://github.com/picrap/VirtualTreeView

namespace VirtualTreeView.Collection
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using Reflection;

    /// <summary>
    /// The flat collection allows to project hierarchical items to a simple list.
    /// This is used by the <see cref="VirtualTreeView"/> to flatten direct or bound items
    /// The source list element are hereafter named "items", and the target list elements are "containers".
    /// </summary>
    public abstract class FlatCollection
    {
        private readonly IList _target;

        /// <summary>
        /// Initializes a new instance of the <see cref="FlatCollection"/> class.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="target">The target.</param>
        /// <exception cref="System.ArgumentNullException">
        /// </exception>
        /// <exception cref="System.ArgumentException">Must be empty</exception>
        /// <exception cref="ArgumentNullException"><paramref name="source" />.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="source" />.</exception>
        /// <exception cref="ArgumentException">Must be empty</exception>
        protected FlatCollection(IList source, IList target)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (target == null)
                throw new ArgumentNullException(nameof(target));
            if (target.Count > 0)
                throw new ArgumentException(@"Must be empty", nameof(target));
            _target = target;
            source.IfType<INotifyCollectionChanged>(nc => nc.CollectionChanged += (o, args) => OnSourceCollectionChanged(null, source, args));
            InsertItems(0, source, null);
        }

        /// <summary>
        /// Clears the collection.
        /// </summary>
        private void Clear()
        {
            _target.Clear();
            _parentsByItems.Clear();
        }

        /// <summary>
        /// Gets a value indicating whether the item is expanded.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns></returns>
        protected abstract bool GetIsExpanded(object item);
        /// <summary>
        /// Gets the item children.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns></returns>
        protected abstract IList GetChildren(object item);

        /// <summary>
        /// Gets the container for item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns></returns>
        protected abstract object GetContainerForItem(object item);
        /// <summary>
        /// Gets the item from container.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <returns></returns>
        protected abstract object GetItemFromContainer(object container);

        /// <summary>
        /// Expands the specified item in the target list.
        /// The children are added to target list, below parent.
        /// This is done recursively, based on items IsExpanded state
        /// This has no effect on any source property, the <see cref="FlatCollection"/> has only read access to source list properties
        /// </summary>
        /// <param name="item">The item.</param>
        public void Expand(object item)
        {
            var itemChildren = GetChildren(item);
            if (IsExpanded(itemChildren))
                return;
            if (itemChildren != null && itemChildren.Count > 0)
            {
                var itemIndex = GetItemIndex(item);
                InsertItems(itemIndex + 1, itemChildren, item);
            }
        }

        /// <summary>
        /// Determines whether the specified item is expanded in the target list.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>
        ///   <c>true</c> if the specified item is expanded; otherwise, <c>false</c>.
        /// </returns>
        public bool IsExpanded(object item)
        {
            return IsExpanded(GetChildren(item));
        }

        private bool IsExpanded(IList itemChildren)
        {
            if (itemChildren == null || itemChildren.Count == 0)
                return false;

            return GetItemIndex(itemChildren[0]) >= 0;
        }

        /// <summary>
        /// Collapses the specified item in the target list.
        /// Removes all children (and sub-children) from target list.
        /// </summary>
        /// <param name="item">The item.</param>
        public void Collapse(object item)
        {
            if (!IsExpanded(item))
                return;
            var itemIndex = GetItemIndex(item);
            var lastChildIndex = GetLastChildIndex(item, false);
            DeleteItems(itemIndex + 1, lastChildIndex - itemIndex);
        }

        private readonly IDictionary<object, object> _parentsByItems = new Dictionary<object, object>();

        /// <summary>
        /// Gets the parent item from given item or null for topmost items
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>The parent or null for topmost items</returns>
        public object GetParent(object item)
        {
            object parent;
            _parentsByItems.TryGetValue(item, out parent);
            return parent;
        }

        private int InsertItem(int index, object item, object parent)
        {
            var count = 1;
            var containerForItem = GetContainerForItem(item);

            _target.Insert(index, containerForItem);
            _parentsByItems[item] = parent;
            var itemChildren = GetChildren(item);
            if (GetIsExpanded(item) && itemChildren != null)
                count += InsertItems(index + 1, itemChildren, item);
            itemChildren.IfType<INotifyCollectionChanged>(c => c.CollectionChanged += (sender, args) => OnSourceCollectionChanged(item, itemChildren, args));
            return count;
        }

        private void OnSourceCollectionChanged(object parent, IList collection, NotifyCollectionChangedEventArgs e)
        {
            if (parent != null && !GetIsExpanded(parent))
                return;
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    InsertItems(GetInsertIndex(collection, e.NewStartingIndex), e.NewItems, parent);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    DeleteItems(e.OldItems);
                    break;
                case NotifyCollectionChangedAction.Replace:
                    throw new NotImplementedException();
                case NotifyCollectionChangedAction.Move:
                    throw new NotImplementedException();
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

        /// <summary>
        /// Deletes the items.
        /// This assumes that items are in sorted order and consecutives
        /// </summary>
        /// <param name="items">The items.</param>
        private void DeleteItems(IEnumerable items)
        {
            // the idea is: delete all items from first item to last item child
            var firstAndLast = items.FirstAndLast();

            var firstIndex = GetItemIndex(firstAndLast[0]);
            var lastChildIndex = GetLastChildIndex(firstAndLast[1], true);

            DeleteItems(firstIndex, lastChildIndex - firstIndex + 1);
        }

        private int InsertItems(int index, IEnumerable items, object parent)
        {
            var startIndex = index;
            foreach (var i in items)
                index += InsertItem(index, i, parent);
            return index - startIndex;
        }

        private void DeleteItems(int index, int count)
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
        private int GetLastChildIndex(object item, bool onlyVisible)
        {
            var itemChildren = GetChildren(item);
            if (itemChildren == null || itemChildren.Count == 0 || (onlyVisible && !GetIsExpanded(item)))
                return GetItemIndex(item);

            return GetLastChildIndex(itemChildren[itemChildren.Count - 1], true);
        }

        /// <summary>
        /// Gets the index of the item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns></returns>
        private int GetItemIndex(object item)
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