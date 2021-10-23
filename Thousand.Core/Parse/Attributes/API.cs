using Superpower;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Thousand.Parse.Attributes
{
    // metadata constructed from various attribute definition groups. this is a 
    // relatively expensive process, but it can be done once and reused repeatedly
    public class API
    {
        public List<AttributeDefinition<AST.DocumentAttribute>> DocumentAttributes { get; }
        public HashSet<string> DocumentNames { get; }
        public List<AttributeDefinition<AST.ObjectAttribute>> ObjectAttributes { get; }
        public HashSet<string> ObjectNames { get; }
        public HashSet<string> ObjectOnlyNames { get; }
        public List<AttributeDefinition<AST.LineAttribute>> LineAttributes { get; }
        public HashSet<string> LineNames { get; }
        public HashSet<string> LineOnlyNames { get; }
        public List<AttributeDefinition<AST.EntityAttribute>> EntityAttributes { get; }
        public HashSet<string> EntityNames { get; }        

        public API()
        {
            DocumentAttributes = DiagramAttributes.All().Select(x => x.Select(x2 => (AST.DocumentAttribute)x2))
                .Concat(RegionAttributes.All().Select(x => x.Select(x2 => (AST.DocumentAttribute)x2)))
                .Concat(TextAttributes.All().Select(x => x.Select(x2 => (AST.DocumentAttribute)x2)))
                .ToList();

            DocumentNames = DocumentAttributes
                .SelectMany(a => a.Names)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            ObjectAttributes = NodeAttributes.All().Select(x => x.Select(x2 => (AST.ObjectAttribute)x2))
                .Concat(RegionAttributes.All().Select(x => x.Select(x2 => (AST.ObjectAttribute)x2)))
                .Concat(TextAttributes.All().Select(x => x.Select(x2 => (AST.ObjectAttribute)x2)))
                .Concat(StrokeAttributes.All().Select(x => x.Select(x2 => (AST.ObjectAttribute)x2)))
                .Concat(PositionAttributes.All().Select(x => x.Select(x2 => (AST.ObjectAttribute)x2)))
                .ToList();

            ObjectNames = ObjectAttributes
                .SelectMany(a => a.Names)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            ObjectOnlyNames = NodeAttributes.All().SelectMany(a => a.Names)
                .Concat(RegionAttributes.All().SelectMany(a => a.Names))
                .Concat(TextAttributes.All().SelectMany(a => a.Names))
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
