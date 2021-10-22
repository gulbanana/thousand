using Superpower;
using System;
using System.Collections.Generic;
using System.Linq;
using Thousand.AST;

namespace Thousand.Parse.Attributes
{
    public class Metadata
    {
        public HashSet<string> DocumentNames { get; }
        public HashSet<string> ObjectNames { get; }
        public HashSet<string> ObjectOnlyNames { get; }
        public List<AttributeDefinition<LineAttribute>> LineAttributes { get; }
        public HashSet<string> LineNames { get; }
        public HashSet<string> LineOnlyNames { get; }
        public HashSet<string> EntityNames { get; }
        public List<AttributeDefinition<AST.EntityAttribute>> EntityAttributes { get; }

        public Metadata()
        {
            DocumentNames = Enum.GetNames<DiagramAttributeKind>()
                .Concat(Enum.GetNames<RegionAttributeKind>())
                .Concat(Enum.GetNames<TextAttributeKind>())
                .Select(Key)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            ObjectNames = Enum.GetNames<NodeAttributeKind>()
                .Concat(Enum.GetNames<RegionAttributeKind>())
                .Concat(Enum.GetNames<TextAttributeKind>())
                .Concat(Enum.GetNames<StrokeAttributeKind>())
                .Concat(Enum.GetNames<PositionAttributeKind>())
                .Select(Key)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            ObjectOnlyNames = Enum.GetNames<NodeAttributeKind>()
                .Concat(Enum.GetNames<RegionAttributeKind>())
                .Concat(Enum.GetNames<TextAttributeKind>())
                .Select(Key)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            LineAttributes = ArrowAttributes.All().Select(x => x.Select(x2 => (AST.LineAttribute)x2))
                .Concat(StrokeAttributes.All().Select(x => x.Select(x2 => (AST.LineAttribute)x2)))
                .Concat(PositionAttributes.All().Select(x => x.Select(x2 => (AST.LineAttribute)x2)))
                .ToList();

            LineNames = LineAttributes
                .SelectMany(a => a.Names)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            LineOnlyNames = ArrowAttributes.All()
                .SelectMany(a => a.Names)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            EntityAttributes = StrokeAttributes.All().Select(x => x.Select(x2 => (AST.EntityAttribute)x2))
                .Concat(PositionAttributes.All().Select(x => x.Select(x2 => (AST.EntityAttribute)x2)))
                .ToList();

            EntityNames = EntityAttributes
                .SelectMany(a => a.Names)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        private static string Key(string k) => Identifier.UnCamel(k).ToLowerInvariant();
    }
}
