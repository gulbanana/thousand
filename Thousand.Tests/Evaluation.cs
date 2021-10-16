using System;
using System.Collections.Generic;
using System.Linq;
using Thousand.Model;
using Xunit;

namespace Thousand.Tests
{
    public class Evaluation
    {
        [Fact]
        public void IntegrationTest()
        {
            var document = new AST.TypedDocument(new AST.TypedDocumentContent[]
            {
                new AST.DiagramAttribute(new AST.RegionFillAttribute(Colour.Blue)),

                new AST.ObjectClass(new("object"), Array.Empty<Parse.Identifier>(), Array.Empty<AST.ObjectAttribute>()),
                new AST.LineClass(new("line"), Array.Empty<Parse.Identifier>(), Array.Empty<AST.SegmentAttribute>()),
                new AST.ObjectClass(new("big"), Array.Empty<Parse.Identifier>(), new AST.ObjectAttribute[] { new AST.TextFontSizeAttribute(50) }), // increases font
                new AST.ObjectClass(new("group"), Array.Empty<Parse.Identifier>(), new AST.ObjectAttribute[] 
                { 
                    new AST.NodeShapeAttribute(null), 
                    new AST.NodeLabelContentAttribute(new Text()), 
                    new AST.RegionLayoutColumnsAttribute(new EqualContentSize()),
                    new AST.RegionLayoutRowsAttribute(new EqualContentSize())
                }),

                new AST.TypedObject(new Parse.Identifier[]{new("big"), new("group")}, null, Array.Empty<AST.ObjectAttribute>(), new AST.TypedObjectContent[] //uses larger font
                {
                    new AST.TypedObject(new Parse.Identifier[]{new("object")}, new("foo"), Array.Empty<AST.ObjectAttribute>(), Array.Empty<AST.TypedObjectContent>()),
                    new AST.TypedObject(new Parse.Identifier[]{new("object")}, new("bar"), new AST.ObjectAttribute[] { new AST.TextFontSizeAttribute(40) }, Array.Empty<AST.TypedObjectContent>()), // reduces font again
                }),

                new AST.TypedObject(new Parse.Identifier[]{new("big")}, new("baz"), Array.Empty<AST.ObjectAttribute>(), Array.Empty<AST.TypedObjectContent>()), //uses larger font
                new AST.TypedObject(new Parse.Identifier[]{new("object")}, new("qux"), Array.Empty<AST.ObjectAttribute>(), Array.Empty<AST.TypedObjectContent>()), 

                // chain containing two edges, both from foo to bar
                new AST.TypedLine(new Parse.Identifier[]{new("line")}, new AST.LineSegment[]{ new(new("foo"), ArrowKind.Forward), new(new("bar"), ArrowKind.Backward), new(new("foo"), null) }, new AST.SegmentAttribute[]{ })
            });

            var state = new GenerationState();
            var result = Evaluator.TryEvaluate(new[] { document }, state, out var root);

            Assert.True(result, state.JoinErrors());
            Assert.False(state.HasWarnings(), state.JoinWarnings());
            Assert.Equal(Colour.Blue, root!.Region.Config.Fill);
            Assert.Equal(5, root.Region.WalkObjects().Count());
            Assert.Equal(3, root.Region.Objects.Count); // group, big baz
            Assert.Equal(2, root.Region.Objects[0].Region.Objects.Count); // group { foo, bar }
            Assert.Equal(2, root.Edges.Count);

            AssertEx.Sequence(root.Region.WalkObjects().Select(o => o.Label).WhereNotNull().Select(l => l.Font.Size), 50, 40, 50, 20);

            AssertEx.Sequence(root.Region.Objects.Select(o => o.Region.Config.Layout.Columns), new EqualContentSize(), new PackedSize(), new PackedSize());
        }
    }
}
