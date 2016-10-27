// VirtualTreeView - a TreeView that *actually* allows virtualization
// https://github.com/picrap/VirtualTreeView

namespace VirtualTreeView.Collection.Reader
{
    using System.Collections;

    /// <summary>
    /// Allows to read enumerables
    /// </summary>
    public abstract class CollectionReader
    {
        /// <summary>
        /// Creates a read for given enumerable.
        /// </summary>
        /// <param name="enumerable">The enumerable.</param>
        /// <returns></returns>
        public static CollectionReader Create(IEnumerable enumerable)
        {
            if (enumerable == null)
                return NullCollectionReader.Instance;
            var list = enumerable as IList;
            if (list != null)
                return new ListCollectionReader(list);
            return new EnumerableCollectionReader(enumerable);
        }

        /// <summary>
        /// Gets a value indicating whether the collection contains at least one element
        /// </summary>
        /// <value>
        ///   <c>true</c> if any; otherwise, <c>false</c>.
        /// </value>
        public abstract bool Any { get; }

        /// <summary>
        /// Gets the first element of collection.
        /// </summary>
        /// <value>
        /// The first.
        /// </value>
        public abstract object First { get; }

        /// <summary>
        /// Gets the last element of collection
        /// </summary>
        /// <value>
        /// The last.
        /// </value>
        public abstract object Last { get; }

        /// <summary>
        /// Gets the element at given index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns></returns>
        public abstract object At(int index);

        /// <summary>
        /// Enumerates all elements from collection
        /// </summary>
        /// <value>
        /// Elements
        /// </value>
        public abstract IEnumerable All { get; }
    }
}