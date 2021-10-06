using System;
using System.Collections.Generic;
using System.Linq;

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

        public static string Join(this IEnumerable<GenerationError> errors)
        {
            return string.Join(Environment.NewLine, errors.Select(e => e.ToString()));
        }
    }
}
