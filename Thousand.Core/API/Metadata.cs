using Superpower;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Thousand.API
{
    // metadata constructed from various attribute definition groups. this is a 
    // relatively expensive process, but it can be done once and reused repeatedly
    public class Metadata
    {
        public List<AttributeDefinition<AST.DocumentAttribute>> DocumentDefinitions { get; }
        public HashSet<string> DocumentNames { get; }
        
        public List<AttributeDefinition<AST.ObjectAttribute>> ObjectDefinitions { get; }
        public HashSet<string> ObjectNames { get; }
        public HashSet<string> ObjectOnlyNames { get; }
        
        public List<AttributeDefinition<AST.LineAttribute>> LineDefinitions { get; }
        public HashSet<string> LineNames { get; }
        public HashSet<string> LineOnlyNames { get; }
        
        public List<AttributeDefinition<AST.EntityAttribute>> EntityDefinitions { get; }
        public HashSet<string> EntityNames { get; }

        public List<AttributeDefinition> ClassAttributes { get; }

        public Dictionary<string, string> Documentation { get; }

        public Metadata()
        {
            DocumentDefinitions = DiagramAttributes.All().Select(x => x.Select(x2 => (AST.DocumentAttribute)x2))
                .Concat(RegionAttributes.All().Select(x => x.Select(x2 => (AST.DocumentAttribute)x2)))
                .Concat(TextAttributes.All().Select(x => x.Select(x2 => (AST.DocumentAttribute)x2)))
                .ToList();

            DocumentNames = DocumentDefinitions
                .SelectMany(a => a.Names)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            ObjectDefinitions = NodeAttributes.All().Select(x => x.Select(x2 => (AST.ObjectAttribute)x2))
                .Concat(RegionAttributes.All().Select(x => x.Select(x2 => (AST.ObjectAttribute)x2)))
                .Concat(TextAttributes.All().Select(x => x.Select(x2 => (AST.ObjectAttribute)x2)))
                .Concat(EntityAttributes.All().Select(x => x.Select(x2 => (AST.ObjectAttribute)x2)))
                .ToList();

            ObjectNames = ObjectDefinitions
                .SelectMany(a => a.Names)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            ObjectOnlyNames = NodeAttributes.All().SelectMany(a => a.Names)
                .Concat(RegionAttributes.All().SelectMany(a => a.Names))
                .Concat(TextAttributes.All().SelectMany(a => a.Names))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            LineDefinitions = ArrowAttributes.All().Select(x => x.Select(x2 => (AST.LineAttribute)x2))
                .Concat(EntityAttributes.All().Select(x => x.Select(x2 => (AST.LineAttribute)x2)))
                .ToList();

            LineNames = LineDefinitions
                .SelectMany(a => a.Names)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            LineOnlyNames = ArrowAttributes.All()
                .SelectMany(a => a.Names)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            EntityDefinitions = EntityAttributes.All()
                .ToList();

            EntityNames = EntityDefinitions
                .SelectMany(a => a.Names)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            ClassAttributes = ObjectDefinitions.Cast<AttributeDefinition>()
                .Concat(LineDefinitions)
                .Distinct()
                .ToList();

            Documentation = ArrowAttributes.All().Cast<AttributeDefinition>()
                .Concat(DiagramAttributes.All())
                .Concat(NodeAttributes.All())
                .Concat(RegionAttributes.All())
                .Concat(EntityAttributes.All())
                .Concat(TextAttributes.All())
                .Where(attr => attr.Documentation != null)
                .SelectMany(attr => attr.Names.Select(n => (name: n, doc: attr.Documentation!)))
                .ToDictionary(t => t.name, t => t.doc, StringComparer.OrdinalIgnoreCase);
        }
    }
}
