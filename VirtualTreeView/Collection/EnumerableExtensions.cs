// VirtualTreeView - a TreeView that *actually* allows virtualization
// https://github.com/picrap/VirtualTreeView

namespace VirtualTreeView.Collection
{
    using System.Collections;

    /// <summary>
    /// Extensions to IEnumerable
    /// </summary>
    public static class EnumerableExtensions
    {
        /// <summary>
        /// Gets the first and last item of collection.
        /// </summary>
        /// <param name="enumerable">The enumerable.</param>
        /// <returns>An array with first and last elements, or null if collection is empty</returns>
        public static object[] FirstAndLast(this IEnumerable enumerable)
        {
            var list = enumerable as IList;
            if (list != null)
                return FirstAndLast(list);

            // now run enumeration
            var e = enumerable.GetEnumerator();
            e.Reset();
            if (!e.MoveNext())
                return null;

            object first = e.Current, last;
            do
            {
                last = e.Current;
            } while (e.MoveNext());

            return new[] { first, last };
        }

        /// <summary>
        /// Gets the first and last item of list.
        /// </summary>
        /// <param name="list">The list.</param>
        /// <returns>An array with first and last elements, or null if collection is empty</returns>
        public static object[] FirstAndLast(this IList list)
        {
            if (list.Count == 0)
                return null;

            return new[] { list[0], list[list.Count - 1] };
        }

        /// <summary>
        /// Gets a value indicating whether the enumerable has elements.
        /// </summary>
        /// <param name="enumerable">The enumerable.</param>
        /// <returns></returns>
        public static bool Any(this IEnumerable enumerable)
        {
            var list = enumerable as IList;
            if (list != null)
                return list.Count > 0;

            var e = enumerable.GetEnumerator();
            e.Reset();
            return e.MoveNext();
        }
    }
}