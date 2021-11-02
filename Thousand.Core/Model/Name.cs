using Superpower.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Thousand.Model
{
    public class Name
    {
        public string AsKey { get; }
        public TextSpan AsLoc { get; }
        public string AsMap(IReadOnlyDictionary<string, TextSpan> sourceMap) { var spanText = AsLoc.ToStringValue(); return sourceMap.ContainsKey(spanText) ? sourceMap[spanText].ToStringValue() : spanText; }

        public Name(string text, TextSpan span) 
        {
            AsKey = text;
            AsLoc = span;
        }

        [Obsolete("Use only in tests"), EditorBrowsable(EditorBrowsableState.Never)]
        public Name(string text)
        {
            AsKey = text;
            AsLoc = new TextSpan(text);
        }

        public override string ToString()
        {
            return $"{AsKey} ({AsLoc.Position})";
        }
    }
}
