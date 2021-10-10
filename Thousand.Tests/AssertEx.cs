using System;
using System.Collections.Generic;
using System.Linq;
using Thousand.Model;
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

        internal static void Eta(Point expected, Point actual, decimal margin = 1m)
        {
            if (Math.Abs(expected.X - actual.X) >= margin)
            {
                throw new AssertActualExpectedException(expected, actual, "Point was incorrect by at least one pixel");
            }

            if (Math.Abs(expected.Y - actual.Y) >= margin)
            {
                throw new AssertActualExpectedException(expected, actual, "Point was incorrect by at least one pixel");
            }
        }
    }
}
