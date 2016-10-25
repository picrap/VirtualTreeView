// VirtualTreeView - a TreeView that *actually* allows virtualization
// https://github.com/picrap/VirtualTreeView

namespace VirtualTreeView.Collection
{
    using System;
    using System.Collections;
    using System.Collections.Specialized;

    public static class NotifyCollectionChangedExtensions
    {
        public static void OnAddRemove(this INotifyCollectionChanged notifyCollectionChanged, Action<object> onAdd, Action<object> onRemove = null)
        {
            notifyCollectionChanged.CollectionChanged += delegate (object sender, NotifyCollectionChangedEventArgs e)
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
                        break;
                    case NotifyCollectionChangedAction.Reset:
                        if (onAdd != null)
                            foreach (var i in (IEnumerable)notifyCollectionChanged)
                                onAdd(i);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            };
        }
    }
}
