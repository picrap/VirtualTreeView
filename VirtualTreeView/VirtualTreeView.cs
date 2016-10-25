// VirtualTreeView - a TreeView that *actually* allows virtualization
// https://github.com/picrap/VirtualTreeView

namespace VirtualTreeView
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Windows.Markup;
    using Reflection;

    [StyleTypedProperty(Property = nameof(ItemContainerStyle), StyleTargetType = typeof(TreeViewItem))]
    [ContentProperty(nameof(HierarchicalItems))]
    public class VirtualTreeView : ItemsControl
    {
        public static readonly DependencyProperty HierarchicalItemsSourceProperty = DependencyProperty.Register(
            nameof(HierarchicalItemsSource), typeof(TreeViewItemCollection), typeof(VirtualTreeView), new PropertyMetadata(null, OnHierarchicalItemsSourceChanged));

        public TreeViewItemCollection HierarchicalItemsSource
        {
            get { return (TreeViewItemCollection)GetValue(HierarchicalItemsSourceProperty); }
            set { SetValue(HierarchicalItemsSourceProperty, value); }
        }

        public IList HierarchicalItems { get; } = new ObservableCollection<object>();

        static VirtualTreeView()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(VirtualTreeView), new FrameworkPropertyMetadata(typeof(VirtualTreeView)));
        }

        public VirtualTreeView()
        {
            HierarchicalItems.IfType<INotifyCollectionChanged>(nc => nc.CollectionChanged += OnHierarchicalItemsCollectionChanged);
        }

        private static void OnHierarchicalItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var @this = (VirtualTreeView)d;
            e.OldValue.IfType<INotifyCollectionChanged>(nc => nc.CollectionChanged -= @this.OnHierarchicalItemsSourceCollectionChanged);
            e.NewValue.IfType<INotifyCollectionChanged>(nc => nc.CollectionChanged += @this.OnHierarchicalItemsSourceCollectionChanged);
        }

        private void OnHierarchicalItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (HierarchicalItemsSource != null)
                throw new InvalidOperationException("HierarchicalItemsSource is data bound, do no use HierarchicalItems");
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    AppendRange(e.NewItems);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    break;
                case NotifyCollectionChangedAction.Replace:
                    break;
                case NotifyCollectionChangedAction.Move:
                    break;
                case NotifyCollectionChangedAction.Reset:
                    Items.Clear();
                    AppendRange(HierarchicalItems);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void OnHierarchicalItemsSourceCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    break;
                case NotifyCollectionChangedAction.Remove:
                    break;
                case NotifyCollectionChangedAction.Replace:
                    break;
                case NotifyCollectionChangedAction.Move:
                    break;
                case NotifyCollectionChangedAction.Reset:
                    Items.Clear();
                    foreach (var i in HierarchicalItemsSource)
                        Append(i);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void Append(object o) => Add(Items.Count, o);
        private void AppendRange(IList l) => AddRange(Items.Count, l);

        private void Add(int index, object o)
        {
            Items.Insert(index, o);
        }

        private void AddRange(int index, IList l)
        {
            foreach (var i in l)
                Items.Insert(index++, i);
        }
    }
}
