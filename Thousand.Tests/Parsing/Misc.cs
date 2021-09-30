﻿using System;
using System.Linq;
using Thousand.Model;
using Thousand.Parse;
using Xunit;

namespace Thousand.Tests.Parsing
{
    public class Misc
    {
        private readonly Superpower.Tokenizer<TokenKind> tokenizer;

        public Misc()
        {
            tokenizer = Tokenizer.Build();            
        }

        [Fact]
        public void AttributeList_Single()
        {
            var tokens = tokenizer.Tokenize(@"[shape=square]");
            var result = Parser.AttributeList(AttributeParsers.NodeAttribute)(tokens);

            Assert.True(result.HasValue, result.ToString());
            AssertEx.Sequence(result.Value, new AST.NodeShapeAttribute(ShapeKind.Square));
        }

        [Fact]
        public void AttributeList_Multiple()
        {
            var tokens = tokenizer.Tokenize(@"[shape=square,shape=oval]");
            var result = Parser.AttributeList(AttributeParsers.NodeAttribute)(tokens);

            Assert.True(result.HasValue, result.ToString());
            AssertEx.Sequence(result.Value, new AST.NodeShapeAttribute(ShapeKind.Square), new AST.NodeShapeAttribute(ShapeKind.Oval));
        }

        [Fact]
        public void AttributeList_Whitespace()
        {
            var tokens = tokenizer.Tokenize(@"[ shape=square,shape = square, shape=square]");
            var result = Parser.AttributeList(AttributeParsers.NodeAttribute)(tokens);

            Assert.True(result.HasValue, result.ToString());
            AssertEx.Sequence(result.Value, new AST.NodeShapeAttribute(ShapeKind.Square), new AST.NodeShapeAttribute(ShapeKind.Square), new AST.NodeShapeAttribute(ShapeKind.Square));
        }

        [Fact]
        public void Object()
        {
            var tokens = tokenizer.Tokenize(@"object foo");
            var result = Parser.Object(tokens);

            Assert.True(result.HasValue, result.ToString());
            AssertEx.Sequence(result.Value.Classes, "object");
            Assert.Equal("foo", result.Value.Name);
        }

        [Fact]
        public void Object_WhiteSpace()
        {
            var tokens = tokenizer.Tokenize(@"   object     foo    ");
            var result = Parser.Object(tokens);

            Assert.True(result.HasValue, result.ToString());
            AssertEx.Sequence(result.Value.Classes, "object");
            Assert.Equal("foo", result.Value.Name);
        }

        [Fact]
        public void Object_Multiline()
        {
            var tokens = tokenizer.Tokenize(@"object ""foo
bar""");
            var result = Parser.Object(tokens);

            Assert.True(result.HasValue, result.ToString());
            AssertEx.Sequence(result.Value.Classes, "object");
            Assert.Equal("foo" + Environment.NewLine + "bar", result.Value.Name);
        }

        [Fact]
        public void Object_Attributed()
        {
            var tokens = tokenizer.Tokenize(@"object ""foo"" [label=""bar""]");
            var result = Parser.Object(tokens);

            Assert.True(result.HasValue, result.ToString());
            Assert.Equal("foo", result.Value.Name);
            AssertEx.Sequence(result.Value.Attributes, new AST.TextLabelAttribute("bar"));
        }

        [Fact]
        public void Object_CustomClass()
        {
            var tokens = tokenizer.Tokenize(@"foo bar");
            var result = Parser.Object(tokens);

            Assert.True(result.HasValue, result.ToString());
            AssertEx.Sequence(result.Value.Classes, "foo");
            Assert.Equal("bar", result.Value.Name);
        }

        [Fact]
        public void Object_CustomClasses()
        {
            var tokens = tokenizer.Tokenize(@"foo.bar baz");
            var result = Parser.Object(tokens);

            Assert.True(result.HasValue, result.ToString());
            AssertEx.Sequence(result.Value.Classes, "foo", "bar");
            Assert.Equal("baz", result.Value.Name);
        }

        [Fact]
        public void Object_Anonymous()
        {
            var tokens = tokenizer.Tokenize(@"object");
            var result = Parser.Object(tokens);

            AssertEx.Sequence(result.Value.Classes, "object");
            Assert.True(result.HasValue, result.ToString());
        }

        [Fact]
        public void Line_Bare()
        {
            var tokens = tokenizer.Tokenize(@"""foo"" -> ""bar""");
            var result = Parser.Edges(tokens);

            Assert.True(result.HasValue, result.ToString());
            Assert.Equal("foo", result.Value.First().Target);
            Assert.Equal("bar", result.Value.Last().Target);
        }

        [Fact]
        public void Line_Attributed()
        {
            var tokens = tokenizer.Tokenize(@"line ""foo"" -> ""bar"" [strokeColour=#000000]");
            var result = Parser.Line(tokens);

            Assert.True(result.HasValue, result.ToString());
            Assert.Equal("foo", result.Value.Elements.First().Target);
            Assert.Equal("bar", result.Value.Elements.Last().Target);
            AssertEx.Sequence(result.Value.Attributes, new AST.StrokeColourAttribute(new Colour(0, 0, 0)));
        }

        [Fact]
        public void Scope_Empty()
        {
            var tokens = tokenizer.Tokenize(@"{}");
            var result = Parser.Scope(tokens);

            Assert.True(result.HasValue, result.ToString());
            Assert.Empty(result.Value);
        }

        [Fact]
        public void Scope_Bare()
        {
            var tokens = tokenizer.Tokenize(@"{
	object foo
    object bar 
    line foo <- bar
}");
            var result = Parser.Scope(tokens);

            Assert.True(result.HasValue, result.ToString());
            Assert.Equal(3, result.Value.Length);
        }

        [Fact]
        public void Scope_InContext()
        {
            var tokens = tokenizer.Tokenize(@"object {
	object foo
    object bar 
    line foo <- bar
}");
            var result = Parser.Object(tokens);

            Assert.True(result.HasValue, result.ToString());
            Assert.Equal(3, result.Value.Children.Length);
        }

        [Fact]
        public void Document_SingleNode()
        {
            var tokens = tokenizer.Tokenize(@"object ""foo""");
            var result = Parser.Document(tokens);

            Assert.True(result.HasValue, result.ToString());
            Assert.Single(result.Value.Declarations);
            Assert.True(result.Value.Declarations.Single().IsT2);
        }

        [Fact]
        public void Document_MultiNode()
        {
            var tokens = tokenizer.Tokenize(@"object ""foo""
object ""bar""
object ""baz""");

            var result = Parser.Document(tokens);

            Assert.True(result.HasValue, result.ToString());
            AssertEx.Sequence(result.Value.Declarations.Where(d => d.IsT2).Select(n => n.AsT2.Name), "foo", "bar", "baz");
        }

        [Fact]
        public void Document_EmptyLines()
        {
            var tokens = tokenizer.Tokenize(@"object ""foo""

object ""bar""");

            var result = Parser.Document(tokens);

            Assert.True(result.HasValue, result.ToString());
            AssertEx.Sequence(result.Value.Declarations.Where(d => d.IsT2).Select(n => n.AsT2.Name), "foo", "bar");
        }

        [Fact]
        public void Document_SingleSeparatedLine()
        {
            var tokens = tokenizer.Tokenize(@"object ""foo""; object ""bar""");

            var result = Parser.Document(tokens);

            Assert.True(result.HasValue, result.ToString());
            AssertEx.Sequence(result.Value.Declarations.Where(d => d.IsT2).Select(n => n.AsT2.Name), "foo", "bar");
        }

        [Fact]
        public void Document_NodesAndEdge()
        {
            var tokens = tokenizer.Tokenize(@"object foo
object bar
line foo -> bar");
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
        public void Declaration_Attribute()
        {
            var tokens = tokenizer.Tokenize(@"fill=black");
            var result = Parser.DocumentDeclaration(tokens);

            Assert.True(result.HasValue);
            Assert.True(result.Value.IsT0);
        }

        [Fact]
        public void Declaration_Class()
        {
            var tokens = tokenizer.Tokenize(@"class foo [stroke=none]");
            var result = Parser.DocumentDeclaration(tokens);

            Assert.True(result.HasValue);
            Assert.True(result.Value.IsT1);
        }

        [Fact]
        public void Declaration_Object()
        {
            var tokens = tokenizer.Tokenize(@"object foo [shape=square]");
            var result = Parser.DocumentDeclaration(tokens);

            Assert.True(result.HasValue);
            Assert.True(result.Value.IsT2);
        }

        [Fact]
        public void Declaration_AnonymousObject()
        {
            var tokens = tokenizer.Tokenize(@"object {}");
            var result = Parser.DocumentDeclaration(tokens);

            Assert.True(result.HasValue);
            Assert.True(result.Value.IsT2);
        }

        [Fact]
        public void Declaration_WhollyAnonymousObject()
        {
            var tokens = tokenizer.Tokenize(@"object");
            var result = Parser.DocumentDeclaration(tokens);

            Assert.True(result.HasValue);
            Assert.True(result.Value.IsT2);
        }

        [Fact]
        public void Declaration_Line()
        {
            var tokens = tokenizer.Tokenize(@"line foo->bar [offset=(0,0)]");
            var result = Parser.DocumentDeclaration(tokens);

            Assert.True(result.HasValue);
            Assert.True(result.Value.IsT3);
        }

        [Fact]
        public void Declaration_All()
        {
            var tokens = tokenizer.Tokenize(@"fill=black
class foo [stroke=none]
object foo [shape=square]
line foo->bar [offset=(0,0)]");
            var result = Parser.Document(tokens);

            Assert.True(result.HasValue);
            Assert.Equal(4, result.Value.Declarations.Length);
            Assert.True(result.Value.Declarations[0].IsT0);
            Assert.True(result.Value.Declarations[1].IsT1);
            Assert.True(result.Value.Declarations[2].IsT2);
            Assert.True(result.Value.Declarations[3].IsT3);
        }

        [Fact]
        public void Declaration_BlankLines()
        {
            var tokens = tokenizer.Tokenize(@"fill=black


class foo [stroke=none]");
            var result = Parser.Document(tokens);

            Assert.True(result.HasValue);
            Assert.Equal(2, result.Value.Declarations.Length);
            Assert.True(result.Value.Declarations[0].IsT0);
            Assert.True(result.Value.Declarations[1].IsT1);
        }
    }
}