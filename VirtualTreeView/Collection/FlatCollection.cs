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
    public class FlatCollection
    {
        private readonly IHierarchicalSource _hierarchicalSource;
        private readonly IList _target;
        private readonly IDictionary<object, FlatNode> _nodes = new Dictionary<object, FlatNode>();
        private readonly FlatNode _rootNode;
        private readonly IDictionary<IEnumerable, FlatNode> _nodesByChildren = new Dictionary<IEnumerable, FlatNode>();

        /// <summary>
        /// Initializes a new instance of the <see cref="FlatCollection" /> class.
        /// </summary>
        /// <param name="hierarchicalSource">The hierarchical source.</param>
        /// <param name="target">The target.</param>
        /// <exception cref="ArgumentNullException">
        /// </exception>
        /// <exception cref="ArgumentException">Must be empty</exception>
        /// <exception cref="System.ArgumentNullException"></exception>
        /// <exception cref="System.ArgumentException">Must be empty</exception>
        public FlatCollection(IHierarchicalSource hierarchicalSource, IList target)
        {
            if (hierarchicalSource == null)
                throw new ArgumentNullException(nameof(hierarchicalSource));
            if (target == null)
                throw new ArgumentNullException(nameof(target));
            if (target.Count > 0)
                throw new ArgumentException(@"Must be empty", nameof(target));
            _hierarchicalSource = hierarchicalSource;
            _target = target;
            _rootNode = new FlatNode(null, null, true);

            SetChildrenSource(_rootNode, _hierarchicalSource.Source);
            InsertRange(_rootNode, _rootNode.ChildrenSource, 0);
        }
        
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

        private void ExpandNode(FlatNode itemNode)
        {
            if (itemNode.IsExpanded)
                return;

            itemNode.IsExpanded = true;

            var itemChildren = itemNode.Parent == null ? _hierarchicalSource.Source : _hierarchicalSource.GetChildren(itemNode.Item);
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

        private void CollapseNode(FlatNode itemNode)
        {
            if (!itemNode.IsExpanded)
                return;

            itemNode.IsExpanded = false;

            Delete(itemNode);
        }

        private void Delete(FlatNode[] nodes)
        {
            if (nodes.Length == 0)
                return;

            foreach (var node in nodes.ToArray())
            {
                Delete(node);

                var nodeIndex = node.Parent.VisualChildren.IndexOf(node);
                var index = GetNodeIndex(node.Parent, nodeIndex);
                _target.RemoveAt(index);
                _nodes.Remove(node.Item);
                node.Parent.RemoveVisualChild(nodeIndex);
            }
        }

        private void Delete(FlatNode node)
        {
            Delete(node.VisualChildren.ToArray());
            SetChildrenSource(node, null);
        }

        private void DeleteRange(IEnumerable items)
        {
            var itemsNodes = items.Cast<object>().Select(i => _nodes[i]).ToArray();
            Delete(itemsNodes);
        }

        /// <summary>
        /// Gets the parent item from given item or null for topmost items
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>The parent or null for topmost items</returns>
        public object GetParent(object item)
        {
            FlatNode node;
            if (!_nodes.TryGetValue(item, out node))
                return null;
            return node.Parent?.Item;
        }

        private void InsertRange(FlatNode parentNode, IEnumerable items, int itemIndex)
        {
            foreach (var item in items)
                Insert(parentNode, item, itemIndex++);
        }

        private void Insert(FlatNode parentNode, object item, int itemIndex)
        {
            var insertIndex = GetNodeIndex(parentNode, itemIndex);
            _target.Insert(insertIndex, _hierarchicalSource.GetContainerForItem(item));

            var itemNode = new FlatNode(item, parentNode, _hierarchicalSource.IsExpanded(item));
            parentNode.InsertVisualChild(itemNode, itemIndex);
            _nodes[item] = itemNode;

            if (itemNode.IsExpanded)
            {
                var itemChildren = _hierarchicalSource.GetChildren(item);
                if (itemChildren != null)
                {
                    SetChildrenSource(itemNode, itemChildren);
                    InsertRange(itemNode, itemChildren, 0);
                }
            }
        }

        /// <summary>
        /// Gets the index of the node (where it must be inserted or removed).
        /// </summary>
        /// <param name="parentNode">The parent node.</param>
        /// <param name="itemIndex">Index of the item.</param>
        /// <returns></returns>
        private static int GetNodeIndex(FlatNode parentNode, int itemIndex)
        {
            // node index is parent node + 1 (parent node itself) + previous siblings size
            var nodeIndex = parentNode.Index + 1 + parentNode.GetChildOffset(itemIndex);
            return nodeIndex;
        }

        private void SetChildrenSource(FlatNode itemNode, IEnumerable itemChildren)
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
            FlatNode node;
            // TODO: this should never happen
            if (!_nodesByChildren.TryGetValue((IEnumerable)itemChildren, out node))
                return;
            OnSourceCollectionChanged(node, e);
        }

        private void OnSourceCollectionChanged(FlatNode itemNode, NotifyCollectionChangedEventArgs e)
        {
            // at any level, a parent may be collapsed, so we won't update anything at all here
            for (var ancestor = itemNode; ancestor != null; ancestor = ancestor.Parent)
                if (!ancestor.IsExpanded)
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