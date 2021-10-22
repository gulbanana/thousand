using System;
using System.Collections.Generic;
using System.Linq;

namespace Thousand.Parse.Attributes
{
    public class Metadata
    {
        public HashSet<string> Documents { get; }
        public HashSet<string> Objects { get; }
        public HashSet<string> ObjectsOnly { get; }
        public HashSet<string> Lines { get; }
        public HashSet<string> LinesOnly { get; }
        public HashSet<string> Entities { get; }

        public Metadata()
        {
            Documents = Enum.GetNames<DiagramAttributeKind>()
                .Concat(Enum.GetNames<RegionAttributeKind>())
                .Concat(Enum.GetNames<TextAttributeKind>())
                .Select(Key)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            Objects = Enum.GetNames<NodeAttributeKind>()
                .Concat(Enum.GetNames<RegionAttributeKind>())
                .Concat(Enum.GetNames<TextAttributeKind>())
                .Concat(Enum.GetNames<LineAttributeKind>())
                .Concat(Enum.GetNames<PositionAttributeKind>())
                .Select(Key)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            ObjectsOnly = Enum.GetNames<NodeAttributeKind>()
                .Concat(Enum.GetNames<RegionAttributeKind>())
                .Concat(Enum.GetNames<TextAttributeKind>())
                .Select(Key)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            Lines = Enum.GetNames<ArrowAttributeKind>()
                .Concat(Enum.GetNames<LineAttributeKind>())
                .Concat(Enum.GetNames<PositionAttributeKind>())
                .Select(Key)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            LinesOnly = Enum.GetNames<ArrowAttributeKind>()
                .Select(Key)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            Entities = Enum.GetNames<LineAttributeKind>()
                .Concat(Enum.GetNames<PositionAttributeKind>())
                .Select(Key)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        private static string Key(string k) => Identifier.UnCamel(k).ToLowerInvariant();
    }
}
