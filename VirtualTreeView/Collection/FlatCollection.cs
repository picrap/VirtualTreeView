// VirtualTreeView - a TreeView that *actually* allows virtualization
// https://github.com/picrap/VirtualTreeView

namespace VirtualTreeView.Collection
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Linq;
    using Reflection;

    /// <summary>
    /// The flat collection allows to project hierarchical items to a simple list.
    /// This is used by the <see cref="VirtualTreeView"/> to flatten direct or bound items
    /// The source list element are hereafter named "items", and the target list elements are "containers".
    /// </summary>
    public abstract class FlatCollection
    {
        private readonly IList _target;
        private readonly IDictionary<object, Node> _nodes = new Dictionary<object, Node>();
        private readonly Node _rootNode;
        private readonly IDictionary<IEnumerable, Node> _nodesByChildren = new Dictionary<IEnumerable, Node>();

        /// <summary>
        /// This is a visual node
        /// </summary>
        private class Node
        {
            public object Item;
            public Node Parent;

            /// <summary>
            /// The visual children
            /// </summary>
            public IList<Node> VisualChildren { get; } = new List<Node>();

            public IEnumerable ChildrenSource;
            public bool IsExpanded;
            public int Size => VisualChildren != null ? VisualChildren.Sum(c => c.Size) + 1 : 1; // +1 because size includes self

            /// <summary>
            /// Gets the child offset, related to parent.
            /// </summary>
            /// <param name="childIndex">Index of the child.</param>
            /// <returns></returns>
            public int GetChildOffset(int childIndex)
            {
                return VisualChildren.Take(childIndex).Sum(c => c.Size);
            }

            /// <summary>
            /// Gets the flat index of the given node.
            /// </summary>
            /// <returns></returns>
            public int GetIndex()
            {
                var parent = Parent;
                if (parent == null) // this is the root node
                    return -1; // which does not exist
                var childIndex = parent.VisualChildren.IndexOf(this);
                var childOffset = parent.GetChildOffset(childIndex);
                // parent to child + parent + parent index
                return childOffset + 1 + parent.GetIndex();
            }
        }

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
        protected FlatCollection(IEnumerable source, IList target)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (target == null)
                throw new ArgumentNullException(nameof(target));
            if (target.Count > 0)
                throw new ArgumentException(@"Must be empty", nameof(target));
            _target = target;
            _rootNode = new Node { IsExpanded = true };
            _nodesByChildren[source] = _rootNode;
            SetChildrenSource(_rootNode, source);
            LoadInitialValuesFromConstructor();
        }

        /// <summary>
        /// Loads the initial values from constructor.
        /// </summary>
        protected virtual void LoadInitialValuesFromConstructor()
        {
            LoadInitialValues();
        }

        /// <summary>
        /// Loads the initial values.
        /// </summary>
        protected void LoadInitialValues()
        {
            InsertRange(_rootNode, _rootNode.ChildrenSource, 0);
        }

        /// <summary>
        /// Clears the collection.
        /// </summary>
        private void Clear()
        {
            _target.Clear();
            _nodesByChildren.Clear();
            _nodes.Clear();
            _rootNode.VisualChildren.Clear();
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
        protected abstract IEnumerable GetChildren(object item);

        /// <summary>
        /// Gets the container for item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns></returns>
        protected abstract object GetContainerForItem(object item);

        /// <summary>
        /// Expands the specified item in the target list.
        /// The children are added to target list, below parent.
        /// This is done recursively, based on items IsExpanded state
        /// This has no effect on any source property, the <see cref="FlatCollection"/> has only read access to source list properties
        /// </summary>
        /// <param name="item">The item.</param>
        public void Expand(object item)
        {
            var itemNode = _nodes[item];
            ExpandNode(itemNode);
        }

        private void ExpandNode(Node itemNode)
        {
            if (itemNode.IsExpanded)
                return;

            itemNode.IsExpanded = true;

            var itemChildren = GetChildren(itemNode.Item);
            SetChildrenSource(itemNode, itemChildren);
            if (itemChildren != null)
                InsertRange(itemNode, itemChildren, 0);
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
            var itemNode = _nodes[item];
            return itemNode.IsExpanded;
        }

        /// <summary>
        /// Collapses the specified item in the target list.
        /// Removes all children (and sub-children) from target list.
        /// </summary>
        /// <param name="item">The item.</param>
        public void Collapse(object item)
        {
            var itemNode = _nodes[item];
            CollapseNode(itemNode);
        }

        private void CollapseNode(Node itemNode)
        {
            if (!itemNode.IsExpanded)
                return;

            Delete(itemNode.VisualChildren);
            itemNode.IsExpanded = false;
        }


        private void Delete(IList<Node> nodes)
        {
            if (nodes.Count == 0)
                return;

            var index = nodes[0].GetIndex();
            var count = nodes.Sum(n => n.Size);
            RemoveRange(index, count);
            foreach (var node in nodes.ToArray())
            {
                node.Parent.VisualChildren.Remove(node);
                Unlink(node);
            }
        }

        private void DeleteRange(IEnumerable items)
        {
            var itemsNodes = items.Cast<object>().Select(i => _nodes[i]).ToArray();
            Delete(itemsNodes);
        }

        private void RemoveRange(int index, int count)
        {
            while (count-- > 0)
                _target.RemoveAt(index);
        }

        private void Unlink(Node node)
        {
            if (node.VisualChildren != null)
                foreach (var child in node.VisualChildren)
                    Unlink(child);

            if (node.ChildrenSource != null)
            {
                _nodesByChildren.Remove(node.ChildrenSource);
                node.ChildrenSource.IfType<INotifyCollectionChanged>(c => c.CollectionChanged -= OnSourceCollectionChanged);
            }
            _nodes.Remove(node.Item);
        }

        /// <summary>
        /// Gets the parent item from given item or null for topmost items
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>The parent or null for topmost items</returns>
        public object GetParent(object item)
        {
            Node node;
            if (!_nodes.TryGetValue(item, out node))
                return null;
            return node.Parent?.Item;
        }

        private void InsertRange(Node parentNode, IEnumerable items, int itemIndex)
        {
            foreach (var item in items)
                Insert(parentNode, item, itemIndex++);
        }

        private void Insert(Node parentNode, object item, int itemIndex)
        {
            // insert index is parent node + 1 (parent node itself) + previous siblings size
            var insertIndex = parentNode.GetIndex() + 1 + parentNode.GetChildOffset(itemIndex);
            _target.Insert(insertIndex, GetContainerForItem(item));

            var itemNode = new Node { Item = item, Parent = parentNode, IsExpanded = GetIsExpanded(item) };
            parentNode.VisualChildren.Insert(itemIndex, itemNode);
            _nodes[item] = itemNode;

            if (itemNode.IsExpanded)
            {
                var itemChildren = GetChildren(item);
                if (itemChildren != null)
                {
                    SetChildrenSource(itemNode, itemChildren);
                    _nodesByChildren[itemChildren] = itemNode;
                    InsertRange(itemNode, itemChildren, 0);
                }
            }
        }

        private void SetChildrenSource(Node itemNode, IEnumerable itemChildren)
        {
            if (ReferenceEquals(itemNode.ChildrenSource, itemChildren))
                return;
            if (itemNode.ChildrenSource != null)
                _nodesByChildren.Remove(itemNode.ChildrenSource);
            itemNode.ChildrenSource.IfType<INotifyCollectionChanged>(c => c.CollectionChanged -= OnSourceCollectionChanged);
            itemNode.ChildrenSource = itemChildren;
            itemNode.ChildrenSource.IfType<INotifyCollectionChanged>(c => c.CollectionChanged += OnSourceCollectionChanged);
            if (itemNode.ChildrenSource != null)
                _nodesByChildren[itemNode.ChildrenSource] = itemNode;
        }

        private void OnSourceCollectionChanged(object itemChildren, NotifyCollectionChangedEventArgs e)
        {
            //var item = _ownersByCollections[(IEnumerable)itemChildren];
            var node = _nodesByChildren[(IEnumerable)itemChildren];
            OnSourceCollectionChanged(node, e);
        }

        private void OnSourceCollectionChanged(Node itemNode, NotifyCollectionChangedEventArgs e)
        {
            if (!itemNode.IsExpanded)
                return;
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    InsertRange(itemNode, e.NewItems, e.NewStartingIndex);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    DeleteRange(e.OldItems);
                    break;
                case NotifyCollectionChangedAction.Replace:
                    DeleteRange(e.OldItems);
                    InsertRange(itemNode, e.NewItems, e.NewStartingIndex);
                    break;
                case NotifyCollectionChangedAction.Move:
                    // :confounded:
                    throw new NotImplementedException();
                case NotifyCollectionChangedAction.Reset:
                    CollapseNode(itemNode);
                    ExpandNode(itemNode);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}