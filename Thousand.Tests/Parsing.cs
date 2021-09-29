using System;
using System.Linq;
using Thousand.Model;
using Thousand.Parse;
using Xunit;

namespace Thousand.Tests
{
    public class Parsing
    {
        private readonly Superpower.Tokenizer<TokenKind> tokenizer;

        public Parsing()
        {
            tokenizer = Tokenizer.Build();            
        }

        [Fact]
        public void ValidColour()
        {
            var tokens = tokenizer.Tokenize(@"#ffffff");
            var result = AttributeParsers.ColourValue(tokens);

            Assert.True(result.HasValue, result.ToString());
            Assert.Equal(Colour.White, result.Value);
        }

        [Fact]
        public void ValidColour_Short()
        {
            var tokens = tokenizer.Tokenize(@"#fff");
            var result = AttributeParsers.ColourValue(tokens);

            Assert.True(result.HasValue, result.ToString());
            Assert.Equal(Colour.White, result.Value);
        }

        [Fact]
        public void InvalidColour()
        {
            var tokens = tokenizer.Tokenize(@"#ffff");
            var result = AttributeParsers.ColourValue(tokens);

            Assert.False(result.HasValue);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(100)]
        [InlineData(10000)]
        public void ValidInteger(int v)
        {
            var tokens = tokenizer.Tokenize(v.ToString());
            var result = AttributeParsers.CountingNumberValue(tokens);

            Assert.True(result.HasValue, result.ToString());
            Assert.Equal(v, result.Value);
        }

        [Theory]
        [InlineData("0")]
        [InlineData("-1")]
        public void InvalidInteger(string v)
        {
            var tokens = tokenizer.Tokenize(v);
            var result = AttributeParsers.CountingNumberValue(tokens);

            Assert.False(result.HasValue, result.ToString());
        }

        [Fact]
        public void ValidAttributeList_Single()
        {
            var tokens = tokenizer.Tokenize(@"[shape=square]");
            var result = Parser.AttributeList(AttributeParsers.NodeAttribute)(tokens);

            Assert.True(result.HasValue, result.ToString());
            AssertEx.Sequence(result.Value, new AST.NodeShapeAttribute(ShapeKind.Square));
        }

        [Fact]
        public void ValidAttributeList_Multiple()
        {
            var tokens = tokenizer.Tokenize(@"[shape=square,shape=oval]");
            var result = Parser.AttributeList(AttributeParsers.NodeAttribute)(tokens);

            Assert.True(result.HasValue, result.ToString());
            AssertEx.Sequence(result.Value, new AST.NodeShapeAttribute(ShapeKind.Square), new AST.NodeShapeAttribute(ShapeKind.Oval));
        }

        [Fact]
        public void ValidAttributeList_Whitespace()
        {
            var tokens = tokenizer.Tokenize(@"[ shape=square,shape = square, shape=square]");
            var result = Parser.AttributeList(AttributeParsers.NodeAttribute)(tokens);

            Assert.True(result.HasValue, result.ToString());
            AssertEx.Sequence(result.Value, new AST.NodeShapeAttribute(ShapeKind.Square), new AST.NodeShapeAttribute(ShapeKind.Square), new AST.NodeShapeAttribute(ShapeKind.Square));
        }

        [Fact]
        public void ValidClass()
        {
            var tokens = tokenizer.Tokenize(@"class foo [label=""bar""]");
            var result = Parser.Class(tokens);

            Assert.True(result.HasValue, result.ToString());
            Assert.Equal("foo", result.Value.Name);
            Assert.Empty(result.Value.BaseClasses);
            AssertEx.Sequence(result.Value.Attributes, new AST.TextLabelAttribute("bar"));
        }

        [Fact]
        public void ValidClass_Subclass()
        {
            var tokens = tokenizer.Tokenize(@"class foo : baz");
            var result = Parser.Class(tokens);

            Assert.True(result.HasValue, result.ToString());
            Assert.Equal("foo", result.Value.Name);
            AssertEx.Sequence(result.Value.BaseClasses, "baz");
        }

        [Fact]
        public void ValidClass_SubclassWithMultipleBases()
        {
            var tokens = tokenizer.Tokenize(@"class foo : bar.baz");
            var result = Parser.Class(tokens);

            Assert.True(result.HasValue, result.ToString());
            AssertEx.Sequence(result.Value.BaseClasses, "bar", "baz");
        }

        [Fact]
        public void InvalidClass_SubclassWithoutBase()
        {
            var tokens = tokenizer.Tokenize(@"class foo : [label=""bar""]");
            var result = Parser.Class(tokens);

            Assert.False(result.HasValue, result.ToString());
        }

        [Fact]
        public void ValidBaseClass()
        {
            var tokens = tokenizer.Tokenize(@": bar");
            var result = Parser.BaseClasses(tokens);

            Assert.True(result.HasValue, result.ToString());
            AssertEx.Sequence(result.Value, "bar");
        }

        [Fact]
        public void InvalidBaseClass()
        {
            var tokens = tokenizer.Tokenize(@" : ");
            var result = Parser.BaseClasses(tokens);

            Assert.False(result.HasValue, result.ToString());
        }

        [Fact]
        public void InvalidBaseClass_FollowedByAttributes()
        {
            var tokens = tokenizer.Tokenize(@" : [label=""bar""]");
            var result = Parser.BaseClasses(tokens);

            Assert.False(result.HasValue, result.ToString());
        }

        [Fact]
        public void ValidNode()
        {
            var tokens = tokenizer.Tokenize(@"object foo");
            var result = Parser.Object(tokens);

            Assert.True(result.HasValue, result.ToString());
            AssertEx.Sequence(result.Value.Classes, "object");
            Assert.Equal("foo", result.Value.Name);
        }

        [Fact]
        public void ValidNode_WhiteSpace()
        {
            var tokens = tokenizer.Tokenize(@"   object     foo    ");
            var result = Parser.Object(tokens);

            Assert.True(result.HasValue, result.ToString());
            AssertEx.Sequence(result.Value.Classes, "object");
            Assert.Equal("foo", result.Value.Name);
        }

        [Fact]
        public void ValidNode_Multiline()
        {
            var tokens = tokenizer.Tokenize(@"object ""foo
bar""");
            var result = Parser.Object(tokens);

            Assert.True(result.HasValue, result.ToString());
            AssertEx.Sequence(result.Value.Classes, "object");
            Assert.Equal("foo" + Environment.NewLine + "bar", result.Value.Name);
        }

        [Fact]
        public void ValidNode_Attributed()
        {
            var tokens = tokenizer.Tokenize(@"object ""foo"" [label=""bar""]");
            var result = Parser.Object(tokens);

            Assert.True(result.HasValue, result.ToString());
            Assert.Equal("foo", result.Value.Name);
            AssertEx.Sequence(result.Value.Attributes, new AST.TextLabelAttribute("bar"));
        }

        [Fact]
        public void ValidEdges()
        {
            var tokens = tokenizer.Tokenize(@"""foo"" -> ""bar""");
            var result = Parser.Edges(tokens);

            Assert.True(result.HasValue, result.ToString());
            Assert.Equal("foo", result.Value.First().Target);
            Assert.Equal("bar", result.Value.Last().Target);
        }

        [Fact]
        public void ValidEdges_Attributed()
        {
            var tokens = tokenizer.Tokenize(@"""foo"" -> ""bar"" [strokeColour=#000000]");
            var result = Parser.AttributedEdges(tokens);

            Assert.True(result.HasValue, result.ToString());
            Assert.Equal("foo", result.Value.Elements.First().Target);
            Assert.Equal("bar", result.Value.Elements.Last().Target);
            AssertEx.Sequence(result.Value.Attributes, new AST.LineStrokeColourAttribute(new Colour(0, 0, 0)));
        }

        [Fact]
        public void ValidNode_CustomClass()
        {
            var tokens = tokenizer.Tokenize(@"foo bar");
            var result = Parser.Object(tokens);

            Assert.True(result.HasValue, result.ToString());
            AssertEx.Sequence(result.Value.Classes, "foo");
            Assert.Equal("bar", result.Value.Name);
        }

        [Fact]
        public void ValidNode_CustomClasses()
        {
            var tokens = tokenizer.Tokenize(@"foo.bar baz");
            var result = Parser.Object(tokens);

            Assert.True(result.HasValue, result.ToString());
            AssertEx.Sequence(result.Value.Classes, "foo", "bar");
            Assert.Equal("baz", result.Value.Name);
        }

        [Fact]
        public void ValidNode_Anonymous()
        {
            var tokens = tokenizer.Tokenize(@"object");
            var result = Parser.Object(tokens);

            AssertEx.Sequence(result.Value.Classes, "object");
            Assert.True(result.HasValue, result.ToString());
        }

        [Fact]
        public void ValidDocument_SingleNode()
        {
            var tokens = tokenizer.Tokenize(@"object ""foo""");
            var result = Parser.Document(tokens);

            Assert.True(result.HasValue, result.ToString());
            Assert.Single(result.Value.Declarations);
            Assert.True(result.Value.Declarations.Single().IsT2);
        }

        [Fact]
        public void ValidDocument_MultiNode()
        {
            var tokens = tokenizer.Tokenize(@"object ""foo""
object ""bar""
object ""baz""");

            var result = Parser.Document(tokens);

            Assert.True(result.HasValue, result.ToString());
            AssertEx.Sequence(result.Value.Declarations.Where(d => d.IsT2).Select(n => n.AsT2.Name), "foo", "bar", "baz");
        }

        [Fact]
        public void ValidDocument_EmptyLines()
        {
            var tokens = tokenizer.Tokenize(@"object ""foo""

object ""bar""");

            var result = Parser.Document(tokens);

            Assert.True(result.HasValue, result.ToString());
            AssertEx.Sequence(result.Value.Declarations.Where(d => d.IsT2).Select(n => n.AsT2.Name), "foo", "bar");
        }

        [Fact]
        public void ValidDocument_NodesAndEdge()
        {
            var tokens = tokenizer.Tokenize(@"object foo
object bar
foo -> bar");
            var result = Parser.Document(tokens);

            Assert.True(result.HasValue, result.ToString());
            Assert.Collection(result.Value.Declarations,
                d => Assert.True(d.IsT2),
                d => Assert.True(d.IsT2),
                d =>
                {
                    Assert.True(d.IsT3);
                    AssertEx.Sequence(d.AsT3.Elements, new AST.Edge("foo", ArrowKind.Forward), new AST.Edge("bar", null));             
                });
        }

        [Fact]
        public void StrokeShorthand_InvalidKeyword()
        {
            var tokens = tokenizer.Tokenize(@"stroke=square");
            var result = AttributeParsers.LineStrokeAttribute(tokens);

            Assert.False(result.HasValue, result.ToString());
        }

        [Theory, InlineData("#000"), InlineData("black")]
        public void StrokeShorthand_SingleColour(string c)
        {
            var tokens = tokenizer.Tokenize($"stroke={c}");
            var result = AttributeParsers.LineStrokeAttribute(tokens);

            Assert.True(result.HasValue, result.ToString());
            Assert.IsType<AST.LineStrokeAttribute>(result.Value);

            var lsa = (AST.LineStrokeAttribute)result.Value;

            Assert.NotNull(lsa.Colour);
            Assert.Equal(Colour.Black, lsa.Colour);
        }

        [Fact]
        public void StrokeShorthand_SingleWidth()
        {
            var tokens = tokenizer.Tokenize($"stroke=none");
            var result = AttributeParsers.LineStrokeAttribute(tokens);

            Assert.True(result.HasValue, result.ToString());
            Assert.IsType<AST.LineStrokeAttribute>(result.Value);

            var lsa = (AST.LineStrokeAttribute)result.Value;

            Assert.NotNull(lsa.Width);
            Assert.True(lsa.Width is ZeroWidth);
        }

        [Fact]
        public void StrokeShorthand_Multiple()
        {
            var tokens = tokenizer.Tokenize($"stroke=2 black");
            var result = AttributeParsers.LineStrokeAttribute(tokens);

            Assert.True(result.HasValue, result.ToString());
            Assert.IsType<AST.LineStrokeAttribute>(result.Value);

            var lsa = (AST.LineStrokeAttribute)result.Value;

            Assert.NotNull(lsa.Colour);
            Assert.Equal(Colour.Black, lsa.Colour);

            Assert.NotNull(lsa.Width);
            Assert.True(lsa.Width is PositiveWidth(2));

            Assert.Null(lsa.Style);
        }

        [Fact]
        public void StrokeShorthand_All()
        {
            var tokens = tokenizer.Tokenize($"stroke=solid green hairline");
            var result = AttributeParsers.LineStrokeAttribute(tokens);

            Assert.True(result.HasValue, result.ToString());
            Assert.IsType<AST.LineStrokeAttribute>(result.Value);

            var lsa = (AST.LineStrokeAttribute)result.Value;

            Assert.NotNull(lsa.Colour);
            Assert.Equal(Colour.Green, lsa.Colour);

            Assert.NotNull(lsa.Width);
            Assert.True(lsa.Width is HairlineWidth);

            Assert.NotNull(lsa.Style);
            Assert.Equal(StrokeKind.Solid, lsa.Style!.Value);
        }

        [Fact]
        public void StrokeShorthand_InContext()
        {
            var tokens = tokenizer.Tokenize($"object [stroke=dashed black]");
            var result = Parser.Object(tokens);

            Assert.True(result.HasValue, result.ToString());
            Assert.Single(result.Value.Attributes);
            Assert.True(result.Value.Attributes.Single().IsT2);

            var lsa = (AST.LineStrokeAttribute)result.Value.Attributes.Single().AsT2;
            
            Assert.NotNull(lsa.Colour);
            Assert.Null(lsa.Width);
            Assert.NotNull(lsa.Style);
        }
    }
}
