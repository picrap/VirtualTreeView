// VirtualTreeView - a TreeView that *actually* allows virtualization
// https://github.com/picrap/VirtualTreeView

namespace VirtualTreeView.Collection
{
    using System;
    using System.Collections;

    // Remove this?

    /// <summary>
    /// Represents a collection accessible by index
    /// </summary>
    public class IndexedCollection : IList
    {
        private readonly IList _list;

        /// <summary>
        /// Gets or sets the <see cref="System.Object"/> at the specified index.
        /// </summary>
        /// <value>
        /// The <see cref="System.Object"/>.
        /// </value>
        /// <param name="index">The index.</param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public object this[int index]
        {
            get { return _list[index]; }
            set { throw new NotImplementedException(); }
        }

        #region Overrides without interest

        /// <summary>
        /// Gets a value indicating whether the <see cref="T:System.Collections.IList" /> is read-only.
        /// </summary>
        public bool IsReadOnly => _list.IsReadOnly;
        /// <summary>
        /// Gets a value indicating whether the <see cref="T:System.Collections.IList" /> has a fixed size.
        /// </summary>
        public bool IsFixedSize => _list.IsFixedSize;

        /// <summary>
        /// Gets the count.
        /// </summary>
        /// <value>
        /// The count.
        /// </value>
        public int Count => _list.Count;

        /// <summary>
        /// Gets an object that can be used to synchronize access to the <see cref="T:System.Collections.ICollection" />.
        /// </summary>
        public object SyncRoot => _list.SyncRoot;
        /// <summary>
        /// Gets a value indicating whether access to the <see cref="T:System.Collections.ICollection" /> is synchronized (thread safe).
        /// </summary>
        public bool IsSynchronized => _list.IsSynchronized;

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="IndexedCollection"/> class.
        /// </summary>
        /// <param name="list">The list.</param>
        public IndexedCollection(IList list)
        {
            _list = list;
        }

        /// <summary>
        /// Inserts an object at specified index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="o">The o.</param>
        public void Insert(int index, object o)
        {
            _list.Insert(index, o);
        }

        /// <summary>
        /// Clears the collection.
        /// </summary>
        public void Clear()
        {
            _list.Clear();
        }

        /// <summary>
        /// Removes an object at specified index.
        /// </summary>
        /// <param name="index">The index.</param>
        public void RemoveAt(int index)
        {
            _list.RemoveAt(index);
        }

        /// <summary>
        /// Gets index of given object.
        /// </summary>
        /// <param name="o">The o.</param>
        /// <returns></returns>
        public int? IndexOf(object o)
        {
            return IndexOf(i => ReferenceEquals(i, o));
        }

        /// <summary>
        /// Gets index of first object matching condition.
        /// </summary>
        /// <param name="predicate">The predicate.</param>
        /// <returns></returns>
        public int? IndexOf(Func<object, bool> predicate)
        {
            for (int index = 0; index < _list.Count; index++)
            {
                if (predicate(_list[index]))
                    return index;
            }
            return null;
        }

        #region Overrides without interest

        /// <summary>
        /// Copies the elements of the <see cref="T:System.Collections.ICollection" /> to an <see cref="T:System.Array" />, starting at a particular <see cref="T:System.Array" /> index.
        /// </summary>
        /// <param name="array">The one-dimensional <see cref="T:System.Array" /> that is the destination of the elements copied from <see cref="T:System.Collections.ICollection" />. The <see cref="T:System.Array" /> must have zero-based indexing.</param>
        /// <param name="index">The zero-based index in <paramref name="array" /> at which copying begins.</param>
        public void CopyTo(Array array, int index) => _list.CopyTo(array, index);

        /// <summary>
        /// Adds an item to the <see cref="T:System.Collections.IList" />.
        /// </summary>
        /// <param name="value">The object to add to the <see cref="T:System.Collections.IList" />.</param>
        /// <returns>
        /// The position into which the new element was inserted, or -1 to indicate that the item was not inserted into the collection.
        /// </returns>
        public int Add(object value)
        {
            var index = Count;
            Insert(index, value);
            return index;
        }

        /// <summary>
        /// Determines whether the <see cref="T:System.Collections.IList" /> contains a specific value.
        /// </summary>
        /// <param name="value">The object to locate in the <see cref="T:System.Collections.IList" />.</param>
        /// <returns>
        /// true if the <see cref="T:System.Object" /> is found in the <see cref="T:System.Collections.IList" />; otherwise, false.
        /// </returns>
        public bool Contains(object value) => IndexOf(value).HasValue;

        /// <summary>
        /// Determines the index of a specific item in the <see cref="T:System.Collections.IList" />.
        /// </summary>
        /// <param name="value">The object to locate in the <see cref="T:System.Collections.IList" />.</param>
        /// <returns>
        /// The index of <paramref name="value" /> if found in the list; otherwise, -1.
        /// </returns>
        int IList.IndexOf(object value) => IndexOf(value) ?? -1;

        /// <summary>
        /// Removes the first occurrence of a specific object from the <see cref="T:System.Collections.IList" />.
        /// </summary>
        /// <param name="value">The object to remove from the <see cref="T:System.Collections.IList" />.</param>
        public void Remove(object value)
        {
            var index = IndexOf(value);
            if (index.HasValue)
                RemoveAt(index.Value);
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Collections.IEnumerator" /> object that can be used to iterate through the collection.
        /// </returns>
        public IEnumerator GetEnumerator() => _list.GetEnumerator();

        #endregion
    }
}
