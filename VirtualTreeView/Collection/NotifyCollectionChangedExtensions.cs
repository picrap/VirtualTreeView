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
        private static readonly IList NoItem = new ArrayList();

        /// <summary>
        /// Gets the additions.
        /// </summary>
        /// <param name="e">The <see cref="NotifyCollectionChangedEventArgs"/> instance containing the event data.</param>
        /// <param name="sender">The sender.</param>
        /// <returns></returns>
        public static IList GetAddedItems(this NotifyCollectionChangedEventArgs e, object sender)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    return e.NewItems;
                case NotifyCollectionChangedAction.Replace:
                    return e.NewItems;
                case NotifyCollectionChangedAction.Reset:
                    return (IList)sender;
                default:
                    return NoItem;
            }
        }
    }
}
