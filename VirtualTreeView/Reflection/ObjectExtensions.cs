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
        public static void IfType<TTest>(this object o, Action<TTest> action)
        {
            if (o is TTest)
                action((TTest)o);
        }
    }
}