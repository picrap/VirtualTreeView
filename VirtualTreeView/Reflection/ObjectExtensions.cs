// VirtualTreeView - a TreeView that *actually* allows virtualization
// https://github.com/picrap/VirtualTreeView

namespace VirtualTreeView.Reflection
{
    using System;

    /// <summary>
    /// Extensions to <see cref="object"/>
    /// </summary>
    public static class ObjectExtensions
    {
        /// <summary>
        /// Performs an action if provided object is of requested type.
        /// (for lazy people like me)
        /// </summary>
        /// <typeparam name="TTest">The type of the test.</typeparam>
        /// <param name="o">The o.</param>
        /// <param name="action">The action.</param>
        public static void IfType<TTest>(this object o, Action<TTest> action)
        {
            if (o is TTest)
                action((TTest)o);
        }
    }
}