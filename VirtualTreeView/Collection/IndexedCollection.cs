// VirtualTreeView - a TreeView that *actually* allows virtualization
// https://github.com/picrap/VirtualTreeView

namespace VirtualTreeView.Collection
{
    using System;
    using System.Collections;

    // Can this be optimized?

    /// <summary>
    /// Represents a collection accessible by index
    /// </summary>
    public class IndexedCollection : IList
    {
        private readonly IList _list;

        public object this[int index]
        {
            get { return _list[index]; }
            set { throw new NotImplementedException(); }
        }

        #region Overrides without interest

        public bool IsReadOnly => _list.IsReadOnly;
        public bool IsFixedSize => _list.IsFixedSize;

        /// <summary>
        /// Gets the count.
        /// </summary>
        /// <value>
        /// The count.
        /// </value>
        public int Count => _list.Count;

        public object SyncRoot => _list.SyncRoot;
        public bool IsSynchronized => _list.IsSynchronized;

        #endregion

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

        public void CopyTo(Array array, int index) => _list.CopyTo(array, index);

        public int Add(object value)
        {
            var index = Count;
            Insert(index, value);
            return index;
        }

        public bool Contains(object value) => IndexOf(value).HasValue;

        int IList.IndexOf(object value) => IndexOf(value) ?? -1;

        public void Remove(object value)
        {
            var index = IndexOf(value);
            if (index.HasValue)
                RemoveAt(index.Value);
        }

        public IEnumerator GetEnumerator() => _list.GetEnumerator();

        #endregion
    }
}
