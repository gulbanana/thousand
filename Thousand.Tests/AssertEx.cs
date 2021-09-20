using System.Collections.Generic;
using Xunit;
using Xunit.Sdk;

namespace Thousand.Tests
{
    static class AssertEx
    {
        public static void Sequence<T>(IEnumerable<T> actual, params T[] expected)
        {
            Assert.Equal(expected, actual);
        }

        public static void Fail(string message)
        {
            throw new XunitException(message);
        }
    }
}
