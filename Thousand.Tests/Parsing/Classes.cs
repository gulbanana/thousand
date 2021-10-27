using System.Linq;
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
            var result = Typed.Class(tokens);

            Assert.True(result.HasValue, result.ToString());
            Assert.Equal("foo", result.Value.Name.Text);
            Assert.Empty(result.Value.BaseClasses);
        }

        [Fact]
        public void Empty_Untyped()
        {
            var tokens = tokenizer.Tokenize(@"class foo");
            var result = Untyped.Class(tokens);

            Assert.True(result.HasValue, result.ToString());
            Assert.Equal("foo", result.Value.Name.Text);
            Assert.Empty(result.Value.BaseClasses);
        }

        [Fact]
        public void Subclass()
        {
            var tokens = tokenizer.Tokenize(@"class foo : baz");
            var result = Typed.Class(tokens);

            Assert.True(result.HasValue, result.ToString());
            Assert.Equal("foo", result.Value.Name.Text);
            AssertEx.Sequence(result.Value.BaseClasses.Select(n => n.Text), "baz");
        }

        [Fact]
        public void Subclass_Untyped()
        {
            var tokens = tokenizer.Tokenize(@"class foo : baz");
            var result = Untyped.Class(tokens);

            Assert.True(result.HasValue, result.ToString());
            Assert.Equal("foo", result.Value.Name.Text);
            AssertEx.Sequence(result.Value.BaseClasses.Select(n => n.Value.Name.Text), "baz");
        }

        [Fact]
        public void Subclass_Untyped_WithArguments()
        {
            var tokens = tokenizer.Tokenize(@"class foo : baz(1, foo)");
            var result = Untyped.Class(tokens);

            Assert.True(result.HasValue, result.ToString());
            Assert.Equal("foo", result.Value.Name.Text);
            AssertEx.Sequence(result.Value.BaseClasses.Select(n => n.Value.Name.Text), "baz");
        }

        [Fact]
        public void Subclass_WithMultipleBases()
        {
            var tokens = tokenizer.Tokenize(@"class foo : bar.baz");
            var result = Typed.Class(tokens);

            Assert.True(result.HasValue, result.ToString());
            AssertEx.Sequence(result.Value.BaseClasses.Select(n => n.Text), "bar", "baz");
        }

        [Fact]
        public void Subclass_NoBase()
        {
            var tokens = tokenizer.Tokenize(@"class foo : [label=""bar""]");
            var result = Typed.Class(tokens);

            Assert.False(result.HasValue, result.ToString());
        }

        [Fact]
        public void BaseClassList()
        {
            var tokens = tokenizer.Tokenize(@": bar");
            var result = Shared.BaseClasses(tokens);

            Assert.True(result.HasValue, result.ToString());
            AssertEx.Sequence(result.Value.Select(n => n.Text), "bar");
        }

        [Fact]
        public void BaseClassList_NoBases()
        {
            var tokens = tokenizer.Tokenize(@" : ");
            var result = Shared.BaseClasses(tokens);

            Assert.False(result.HasValue, result.ToString());
        }

        [Fact]
        public void BaseClassList_NoBasesAndAttributes()
        {
            var tokens = tokenizer.Tokenize(@" : [label=""bar""]");
            var result = Shared.BaseClasses(tokens);

            Assert.False(result.HasValue, result.ToString());
        }

        [Fact]
        public void ObjectClass()
        {
            var tokens = tokenizer.Tokenize(@"class foo [shape=square]");
            var result = Typed.Class(tokens);

            Assert.True(result.HasValue, result.ToString());
            Assert.IsType<AST.ObjectClass>(result.Value);
            AssertEx.Sequence(((AST.ObjectClass)result.Value).Attributes, new AST.NodeShapeAttribute(ShapeKind.Square));
        }

        [Fact]
        public void LineClass()
        {
            var tokens = tokenizer.Tokenize(@"class foo [offset-start=1 1]");
            var result = Typed.Class(tokens);

            Assert.True(result.HasValue, result.ToString());
            Assert.IsType<AST.LineClass>(result.Value);
            AssertEx.Sequence(((AST.LineClass)result.Value).Attributes, new AST.ArrowOffsetStartAttribute(new Point(1, 1)));
        }

        [Fact]
        public void ObjectOrLineClass()
        {
            var tokens = tokenizer.Tokenize(@"class foo [stroke-colour=red]");
            var result = Typed.Class(tokens);

            Assert.True(result.HasValue, result.ToString());
            Assert.IsType<AST.ObjectOrLineClass>(result.Value);
            AssertEx.Sequence(((AST.ObjectOrLineClass)result.Value).Attributes, new AST.EntityStrokeColourAttribute(Colour.Red));
        }

        [Fact] 
        public void WithScope()
        {
            var tokens = tokenizer.Tokenize(@"class foo {shape=square}");
            var result = Typed.Class(tokens);

            Assert.True(result.HasValue, result.ToString());
            Assert.IsType<AST.ObjectClass>(result.Value);
            AssertEx.Sequence(((AST.ObjectClass)result.Value).Declarations, (AST.ObjectAttribute)new AST.NodeShapeAttribute(ShapeKind.Square));
        }

        [Fact]
        public void WithScope_AfterAttrs()
        {
            var tokens = tokenizer.Tokenize(@"class foo [shape=rect] {shape=square}");
            var result = Typed.Class(tokens);

            Assert.True(result.HasValue, result.ToString());
            Assert.IsType<AST.ObjectClass>(result.Value);
            AssertEx.Sequence(((AST.ObjectClass)result.Value).Attributes, (AST.ObjectAttribute)new AST.NodeShapeAttribute(ShapeKind.Rect));
            AssertEx.Sequence(((AST.ObjectClass)result.Value).Declarations, (AST.ObjectAttribute)new AST.NodeShapeAttribute(ShapeKind.Square));
        }

        [Fact]
        public void WithScope_AfterAttrs_OLC()
        {
            var tokens = tokenizer.Tokenize(@"class foo [stroke-colour=black] {stroke-colour=black}");
            var result = Typed.Class(tokens);

            Assert.True(result.HasValue, result.ToString());
            Assert.IsType<AST.ObjectClass>(result.Value);
            AssertEx.Sequence(((AST.ObjectClass)result.Value).Attributes, (AST.ObjectAttribute)new AST.EntityStrokeColourAttribute(Colour.Black));
            AssertEx.Sequence(((AST.ObjectClass)result.Value).Declarations, (AST.ObjectAttribute)new AST.EntityStrokeColourAttribute(Colour.Black));
        }

        [Fact]
        public void WithScope_Untyped()
        {
            var tokens = tokenizer.Tokenize(@"class foo {shape=square}");
            var result = Untyped.Class(tokens);

            Assert.True(result.HasValue, result.ToString());
            Assert.NotEmpty(result.Value.Declarations);
        }

        [Fact]
        public void WithScope_Untyped_AfterAttrs()
        {
            var tokens = tokenizer.Tokenize(@"class foo [shape=rect] {shape=square}");
            var result = Untyped.Class(tokens);

            Assert.True(result.HasValue, result.ToString());
            Assert.NotEmpty(result.Value.Declarations);
        }
        
        [Fact]
        public void WithScope_Untyped_Nested_AsDocument()
        {
            var tokens = tokenizer.Tokenize(@"class foo { object { object } } foo");
            var result = Untyped.Document(tokens);

            Assert.True(result.HasValue, result.ToString());
            Assert.Single(result.Value.Declarations.Where(d => d.Value.IsT2));
        }

        [Fact]
        public void Template_MacroArg()
        {
            var tokens = tokenizer.Tokenize(@"class foo($x) [fill=$x]");
            var result = Untyped.Class(tokens);

            Assert.True(result.HasValue, result.ToString());
            Assert.Single(result.Value.Attributes.Where(a => a.Key.Text == "fill"));
        }

        [Fact]
        public void Template_MacroBody()
        {
            var tokens = tokenizer.Tokenize(@"class foo($x) { object $x }");
            var result = Untyped.Class(tokens);

            Assert.True(result.HasValue, result.ToString());
        }
    }
}
