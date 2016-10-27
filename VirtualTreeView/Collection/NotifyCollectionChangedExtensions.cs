// VirtualTreeView - a TreeView that *actually* allows virtualization
// https://github.com/picrap/VirtualTreeView

namespace VirtualTreeView.Collection
{
    using System;
    using System.Collections;
    using System.Collections.Specialized;

    /// <summary>
    /// Extensions to <see cref="INotifyCollectionChanged"/> for lazy boys (and girls, no sexism here)
    /// </summary>
    public static class NotifyCollectionChangedExtensions
    {
        /// <summary>
        /// Sets two methods to be called when elements are added to or removed from collection.
        /// </summary>
        /// <param name="collection">The collection.</param>
        /// <param name="onAdd">A method to be called when element is added.</param>
        /// <param name="onRemove">A method to be called when element is removed.</param>
        public static void OnAddRemove(this INotifyCollectionChanged collection, Action<object> onAdd, Action<object> onRemove = null)
        {
            collection.CollectionChanged += delegate (object sender, NotifyCollectionChangedEventArgs e)
            {
                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        if (onAdd != null)
                            foreach (var i in e.NewItems)
                                onAdd(i);
                        break;
                    case NotifyCollectionChangedAction.Remove:
                        if (onRemove != null)
                            foreach (var i in e.OldItems)
                                onRemove(i);
                        break;
                    case NotifyCollectionChangedAction.Replace:
                        if (onRemove != null)
                            foreach (var i in e.OldItems)
                                onRemove(i);
                        if (onAdd != null)
                            foreach (var i in e.NewItems)
                                onAdd(i);
                        break;
                    case NotifyCollectionChangedAction.Move:
                        throw new NotImplementedException();
                    case NotifyCollectionChangedAction.Reset:
                        if (onAdd != null)
                            foreach (var i in (IEnumerable)collection)
                                onAdd(i);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            };
        }
    }
}
