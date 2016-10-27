// VirtualTreeView - a TreeView that *actually* allows virtualization
// https://github.com/picrap/VirtualTreeView

namespace VirtualTreeView.Collection.Reader
{
    using System.Collections;

    /// <summary>
    /// Specialized reader for <see cref="IList"/>
    /// </summary>
    public class ListCollectionReader : CollectionReader
    {
        private readonly IList _list;

        /// <summary>
        /// Gets a value indicating whether the collection contains at least one element
        /// </summary>
        /// <value>
        ///   <c>true</c> if any; otherwise, <c>false</c>.
        /// </value>
        public override bool Any => _list.Count > 0;

        /// <summary>
        /// Gets the first element of collection.
        /// </summary>
        /// <value>
        /// The first.
        /// </value>
        public override object First => _list[0];

        /// <summary>
        /// Gets the last element of collection
        /// </summary>
        /// <value>
        /// The last.
        /// </value>
        public override object Last => _list[_list.Count - 1];

        /// <summary>
        /// Gets the element at given index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns></returns>
        public override object At(int index) => _list[index];

        /// <summary>
        /// Enumerates all elements from collection
        /// </summary>
        /// <value>
        /// Elements
        /// </value>
        public override IEnumerable All => _list;

        /// <summary>
        /// Initializes a new instance of the <see cref="ListCollectionReader"/> class.
        /// </summary>
        /// <param name="list">The list.</param>
        public ListCollectionReader(IList list)
        {
            _list = list;
        }
    }
}