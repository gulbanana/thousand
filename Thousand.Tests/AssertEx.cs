using System.Collections.Generic;
using Xunit;

namespace Thousand.Tests
{
    static class AssertEx
    {
        public static void Sequence<T>(IEnumerable<T> actual, params T[] expected)
        {
            Assert.Equal(expected, actual);
        }
    }
}
