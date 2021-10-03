using Thousand.Model;
using Thousand.Parse;
using Xunit;

namespace Thousand.Tests.Parsing
{
    public class Classes
    {
        private readonly Superpower.Tokenizer<TokenKind> tokenizer;

        public Classes()
        {
            tokenizer = Tokenizer.Build();            
        }

        [Fact]
        public void Empty()
        {
            var tokens = tokenizer.Tokenize(@"class foo");
            var result = Parser.Class(tokens);

            Assert.True(result.HasValue, result.ToString());
            Assert.Equal("foo", result.Value.Name);
            Assert.Empty(result.Value.BaseClasses);
        }

        [Fact]
        public void Subclass()
        {
            var tokens = tokenizer.Tokenize(@"class foo : baz");
            var result = Parser.Class(tokens);

            Assert.True(result.HasValue, result.ToString());
            Assert.Equal("foo", result.Value.Name);
            AssertEx.Sequence(result.Value.BaseClasses, "baz");
        }

        [Fact]
        public void Subclass_WithMultipleBases()
        {
            var tokens = tokenizer.Tokenize(@"class foo : bar.baz");
            var result = Parser.Class(tokens);

            Assert.True(result.HasValue, result.ToString());
            AssertEx.Sequence(result.Value.BaseClasses, "bar", "baz");
        }

        [Fact]
        public void Subclass_NoBase()
        {
            var tokens = tokenizer.Tokenize(@"class foo : [label=""bar""]");
            var result = Parser.Class(tokens);

            Assert.False(result.HasValue, result.ToString());
        }

        [Fact]
        public void BaseClassList()
        {
            var tokens = tokenizer.Tokenize(@": bar");
            var result = Parser.BaseClasses(tokens);

            Assert.True(result.HasValue, result.ToString());
            AssertEx.Sequence(result.Value, "bar");
        }

        [Fact]
        public void BaseClassList_NoBases()
        {
            var tokens = tokenizer.Tokenize(@" : ");
            var result = Parser.BaseClasses(tokens);

            Assert.False(result.HasValue, result.ToString());
        }

        [Fact]
        public void BaseClassList_NoBasesAndAttributes()
        {
            var tokens = tokenizer.Tokenize(@" : [label=""bar""]");
            var result = Parser.BaseClasses(tokens);

            Assert.False(result.HasValue, result.ToString());
        }

        [Fact]
        public void ObjectClass()
        {
            var tokens = tokenizer.Tokenize(@"class foo [shape=square]");
            var result = Parser.Class(tokens);

            Assert.True(result.HasValue, result.ToString());
            Assert.IsType<AST.ObjectClass>(result.Value);
            AssertEx.Sequence(((AST.ObjectClass)result.Value).Attributes, new AST.NodeShapeAttribute(ShapeKind.Square));
        }

        [Fact]
        public void LineClass()
        {
            var tokens = tokenizer.Tokenize(@"class foo [offsetX=1]");
            var result = Parser.Class(tokens);

            Assert.True(result.HasValue, result.ToString());
            Assert.IsType<AST.LineClass>(result.Value);
            AssertEx.Sequence(((AST.LineClass)result.Value).Attributes, new AST.ArrowOffsetXAttribute(1, 1));
        }

        [Fact]
        public void ObjectOrLineClass()
        {
            var tokens = tokenizer.Tokenize(@"class foo [strokeColour=red]");
            var result = Parser.Class(tokens);

            Assert.True(result.HasValue, result.ToString());
            Assert.IsType<AST.ObjectOrLineClass>(result.Value);
            AssertEx.Sequence(((AST.ObjectOrLineClass)result.Value).Attributes, new AST.StrokeColourAttribute(Colour.Red));
        }
    }
}
