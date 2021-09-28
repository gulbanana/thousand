using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Thousand.Model;

namespace Thousand.Tests
{
    class MockMeasures : IReadOnlyDictionary<string, Point>
    {
        private readonly Point size;

        public MockMeasures(Point size)
        {
            this.size = size;
        }

        public Point this[string key] => size;
        public bool TryGetValue(string key, [MaybeNullWhen(false)] out Point value) { value = size; return true; }

        public bool ContainsKey(string key) => true;

        public int Count => 0;

        public IEnumerable<string> Keys => Enumerable.Empty<string>();
        public IEnumerable<Point> Values => Enumerable.Empty<Point>();

        public IEnumerator<KeyValuePair<string, Point>> GetEnumerator() => Enumerable.Empty<KeyValuePair<string, Point>>().GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => Enumerable.Empty<KeyValuePair<string, Point>>().GetEnumerator();
    }
}
