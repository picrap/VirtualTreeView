// VirtualTreeView - a TreeView that *actually* allows virtualization
// https://github.com/picrap/VirtualTreeView

namespace VirtualTreeView.Collection
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// This is a visual node
    /// </summary>
    public class FlatNode
    {
        /// <summary>
        /// Gets the item.
        /// </summary>
        /// <value>
        /// The item.
        /// </value>
        public object Item { get; }

        /// <summary>
        /// Gets the parent.
        /// </summary>
        /// <value>
        /// The parent.
        /// </value>
        public FlatNode Parent { get; }

        /// <summary>
        /// The visual children
        /// </summary>
        public IList<FlatNode> VisualChildren { get; } = new List<FlatNode>();

        /// <summary>
        /// Gets or sets the children source.
        /// </summary>
        /// <value>
        /// The children source.
        /// </value>
        public IEnumerable ChildrenSource { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is expanded.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is expanded; otherwise, <c>false</c>.
        /// </value>
        public bool IsExpanded { get; set; }

        private int? _size;

        /// <summary>
        /// Gets the size.
        /// </summary>
        /// <value>
        ///   The size.
        /// </value>
        public int Size
        {
            get
            {
                if (!_size.HasValue)
                    _size = VisualChildren != null ? VisualChildren.Sum(c => c.Size) + 1 : 1;
                return _size.Value;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FlatNode" /> class.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="parent">The parent.</param>
        /// <param name="isExpanded">if set to <c>true</c> [is expanded].</param>
        public FlatNode(object item, FlatNode parent, bool isExpanded)
        {
            Item = item;
            Parent = parent;
            IsExpanded = isExpanded;
        }

        /// <summary>
        /// Gets the child offset, related to parent.
        /// </summary>
        /// <param name="childIndex">Index of the child.</param>
        /// <returns></returns>
        public int GetChildOffset(int childIndex) => VisualChildren.Take(childIndex).Sum(c => c.Size);

        private int? _childOffset;

        /// <summary>
        /// Gets the child offset of this node, related to parent.
        /// </summary>
        /// <value>
        /// The child offset.
        /// </value>
        public int ChildOffset
        {
            get
            {
                if (!_childOffset.HasValue)
                    _childOffset = Parent.GetChildOffset(Parent.VisualChildren.IndexOf(this));
                return _childOffset.Value;
            }
        }

        /// <summary>
        /// Gets the flat index of the given node.
        /// </summary>
        /// <returns></returns>
        public int Index
        {
            get
            {
                var parent = Parent;
                if (parent == null) // this is the root node
                    return -1; // which does not exist
                var childOffset = ChildOffset;
                // parent to child + parent + parent index
                return childOffset + 1 + parent.Index;
            }
        }

        /// <summary>
        /// Inserts the visual child.
        /// And invalidates all impacted measures
        /// </summary>
        /// <param name="node">The node.</param>
        /// <param name="index">The index.</param>
        public void InsertVisualChild(FlatNode node, int index)
        {
            VisualChildren.Insert(index, node);
            for (int i = index + 1; i < VisualChildren.Count; i++)
                VisualChildren[i]._childOffset++;
            AdjustAncestorsSize(+1);
        }

        /// <summary>
        /// Removes the visual child.
        /// </summary>
        /// <param name="index">The index.</param>
        public void RemoveVisualChild(int index)
        {
            VisualChildren.RemoveAt(index);
            for (int i = index; i < VisualChildren.Count; i++)
                VisualChildren[i]._childOffset--;
            AdjustAncestorsSize(-1);
        }

        private void AdjustAncestorsSize(int delta)
        {
            for (var ancestor = this; ;)
            {
                ancestor._size += delta;
                var ancestorParent = ancestor.Parent;
                if (ancestorParent == null)
                    break;

                var ancestorIndex = ancestorParent.VisualChildren.IndexOf(ancestor);
                while (++ancestorIndex < ancestorParent.VisualChildren.Count)
                    ancestorParent.VisualChildren[ancestorIndex]._childOffset += delta;

                ancestor = ancestorParent;
            }
        }
    }
}
