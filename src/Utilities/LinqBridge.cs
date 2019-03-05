namespace Dynasor.Utilities
{
    using System;
    using System.Collections.Generic;

    internal static class LinqBridge
    {
        public static Stack<T> ToStack<T>(this IEnumerable<T> collection)
        {
            return new Stack<T>(collection ?? Array.Empty<T>());
        }
    }
}