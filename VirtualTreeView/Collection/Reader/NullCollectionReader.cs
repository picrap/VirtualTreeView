// VirtualTreeView - a TreeView that *actually* allows virtualization
// https://github.com/picrap/VirtualTreeView

namespace VirtualTreeView.Collection.Reader
{
    using System;
    using System.Collections;

    /// <summary>
    /// A big fake to ease null children collections
    /// </summary>
    public class NullCollectionReader : CollectionReader
    {
        /// <summary>
        /// The default instance
        /// </summary>
        public static readonly NullCollectionReader Instance = new NullCollectionReader();

        /// <summary>
        /// Gets a value indicating whether the collection contains at least one element
        /// </summary>
        /// <value>
        ///   <c>true</c> if any; otherwise, <c>false</c>.
        /// </value>
        public override bool Any => false;

        /// <summary>
        /// Gets the first element of collection.
        /// </summary>
        /// <value>
        /// The first.
        /// </value>
        /// <exception cref="System.InvalidOperationException"></exception>
        public override object First
        {
            get { throw new InvalidOperationException(); }
        }

        /// <summary>
        /// Gets the last element of collection
        /// </summary>
        /// <value>
        /// The last.
        /// </value>
        /// <exception cref="System.InvalidOperationException"></exception>
        public override object Last
        {
            get { throw new InvalidOperationException(); }
        }

        /// <summary>
        /// Gets the element at given index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentOutOfRangeException"></exception>
        public override object At(int index)
        {
            throw new ArgumentOutOfRangeException();
        }

        /// <summary>
        /// Enumerates all elements from collection
        /// </summary>
        /// <value>
        /// Elements
        /// </value>
        public override IEnumerable All { get; } = new object[0];
    }
}