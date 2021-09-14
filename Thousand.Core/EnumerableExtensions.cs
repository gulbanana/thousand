using System.Collections.Generic;

namespace Thousand
{
    public static class EnumerableExtensions
    {
        public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> source) where T : class
        {
            foreach (var elem in source)
            {
                if (elem is not null) yield return elem;
            }
        }
    }
}
