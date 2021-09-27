using System.Collections.Generic;
using System.Linq;
using Xunit;
using Xunit.Sdk;

namespace Thousand.Tests
{
    static class AssertEx
    {
        public static void Sequence<T>(IEnumerable<T> actual, params T[] expected)
        {
            Assert.Equal(expected, actual.ToArray());
        }

        public static void Fail(string message)
        {
            throw new XunitException(message);
        }
    }
}
