// VirtualTreeView - a TreeView that *actually* allows virtualization
// https://github.com/picrap/VirtualTreeView

namespace VirtualTreeView
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;

    internal class ItemsControlItemPropertyReader<TValue>
    {
        private class SourceProperty
        {
            public PropertyInfo Property;
            public bool MustUseDependencyProperty;
        }

        private readonly ItemsControl _itemsControl;
        private readonly DependencyProperty _dependencyProperty;
        private readonly TValue _defaultValue;
        private readonly bool _allowSourceProperties;
        private readonly IDictionary<Type, SourceProperty> _sourceProperties = new Dictionary<Type, SourceProperty>();

        public ItemsControlItemPropertyReader(ItemsControl itemsControl, DependencyProperty dependencyProperty, TValue defaultValue = default(TValue), bool allowSourceProperties = true)
        {
            _itemsControl = itemsControl;
            _dependencyProperty = dependencyProperty;
            _defaultValue = defaultValue;
            _allowSourceProperties = allowSourceProperties;
        }

        /// <summary>
        /// Gets the property value for item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns></returns>
        public TValue Get(object item)
        {
            if (item == null)
                return _defaultValue;
            var value = _defaultValue;
            if (GetFromGeneratedContainer(item, ref value) || GetFromSourceProperty(item, ref value))
                return value;
            try
            {
                return GetFromNewContainer(item);
            }
            catch
            {
                return default(TValue);
            }
        }

        /// <summary>
        /// Gets the value from generated item.
        /// This is the most reliable
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        private bool GetFromGeneratedContainer(object item, ref TValue value)
        {
            var container = GetGeneratedItem(item);
            if (container == null)
                return false;

            value = (TValue)container.GetValue(_dependencyProperty);
            return true;
        }

        private DependencyObject GetGeneratedItem(object item)
        {
            return _itemsControl.ItemContainerGenerator.ContainerFromItem(item);
        }

        /// <summary>
        /// Gets the value using direct property reading.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        private bool GetFromSourceProperty(object item, ref TValue value)
        {
            if (!_allowSourceProperties)
                return false;

            var itemType = item.GetType();
            SourceProperty property;
            if (!_sourceProperties.TryGetValue(itemType, out property))
                return false;

            // the property is a complex binding
            if (property.MustUseDependencyProperty)
                return false;

            if (property.Property == null)
            {
                value = _defaultValue;
                return true;
            }

            value = (TValue)property.Property.GetValue(item);
            return true;
        }

        /// <summary>
        /// Gets the value using a created and data bound container
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns></returns>
        private TValue GetFromNewContainer(object item)
        {
            var treeViewItem = CreateContainer(item);

            var itemType = item.GetType();
            if (_allowSourceProperties && !_sourceProperties.ContainsKey(itemType))
            {
                var binding = BindingOperations.GetBinding(treeViewItem, _dependencyProperty);
                if (binding == null)
                    _sourceProperties[itemType] = new SourceProperty { MustUseDependencyProperty = false, Property = null };
                else
                {
                    // when the binding is missing or complex, use from source
                    var useDependencyProperty = binding.Source != null || binding.RelativeSource != null || binding.ElementName != null || binding.Path.Path.Any(IsSpecial);
                    var propertyInfo = itemType.GetProperty(binding.Path.Path);
                    _sourceProperties[itemType] = new SourceProperty { MustUseDependencyProperty = useDependencyProperty, Property = propertyInfo };
                }
            }

            return (TValue)treeViewItem.GetValue(_dependencyProperty);
        }

        private static bool IsSpecial(char c)
        {
            return c == '.' || c == '[';
        }

        /// <summary>
        /// Creates a container when no other alternative is available.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns></returns>
        // Any help for something clean here (or in place of) is welcome
        private FrameworkElement CreateContainer(object item)
        {
            _itemsControl.ApplyTemplate();
            var hierarchicalDataTemplate = GetHirarchicalItemTemplate(item);
            var elementType = _dependencyProperty.OwnerType;
            if (_itemsControl.ItemContainerStyle?.TargetType != null && _itemsControl.ItemContainerStyle.TargetType.IsSubclassOf(elementType))
                elementType = _itemsControl.ItemContainerStyle.TargetType;
            var frameworkElement = (FrameworkElement)Activator.CreateInstance(elementType);
            frameworkElement.DataContext = item;
            if (hierarchicalDataTemplate?.ItemsSource != null)
                BindingOperations.SetBinding(frameworkElement, ItemsControl.ItemsSourceProperty, hierarchicalDataTemplate.ItemsSource);
            // the style, if any, needs to be applied after DataContext is set, otherwise it won't bind
            frameworkElement.Style = _itemsControl.ItemContainerStyle;
            return frameworkElement;
        }

        /// <summary>
        /// Gets the hirarchical item template, if it applies to given item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns></returns>
        private HierarchicalDataTemplate GetHirarchicalItemTemplate(object item)
        {
            if (_itemsControl.ItemTemplate != null)
                return _itemsControl.ItemTemplate as HierarchicalDataTemplate;

            var hdt = (from e in _itemsControl.Resources.OfType<DictionaryEntry>()
                       let h = e.Value as HierarchicalDataTemplate
                       where h != null
                       let k = e.Key as TemplateKey
                       where k != null
                       let t = k.DataType as Type
                       where t != null && t.IsAssignableFrom(item.GetType())
                       select h).FirstOrDefault();
            return hdt;
        }
    }
}
