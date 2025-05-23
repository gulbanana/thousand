﻿using Microsoft.Extensions.Logging.Abstractions;
using OmniSharp.Extensions.LanguageServer.Protocol;
using System.Linq;
using System.Threading;
using Thousand.LSP;
using Thousand.LSP.Analyse;
using Thousand.Model;
using Xunit;

namespace Thousand.Tests.LSP
{
    public class ErrorToleration
    {
        private readonly IDiagnosticService diagnostics;
        private readonly BufferService buffers;
        private readonly AnalysisService semantics;

        public ErrorToleration()
        {
            diagnostics = new MockDiagnosticService();
            buffers = new BufferService();
            semantics = new AnalysisService(NullLogger<AnalysisService>.Instance, new API.Metadata(), buffers, diagnostics, new MockGenerationService());
        }

        private Analysis Parse(string source)
        {
            var key = DocumentUri.From("file://test.1000");
            buffers.Add(key, source);
            return semantics.Analyse(new ServerOptions(), key, CancellationToken.None);
        }

        [Fact]
        public void CorrectSimpleDocument()
        {
            var document = Parse(
@"class foo
foo bar [fill=red]
foo baz [stroke=red]
line bar -- baz [stroke=blue]");

            Assert.NotNull(document.Tokens);
            Assert.NotNull(document.Main);
            Assert.NotNull(document.ValidSyntax);
            Assert.NotNull(document.Root);
            Assert.NotNull(document.Diagram);

            Assert.Equal(4, document.Main!.Syntax.Declarations.Count(d => d.Value is not AST.InvalidDeclaration));
        }

        [Fact]
        public void CorrectComplexDocument()
        {
            var document = Parse(
@"class object
class template($x) [stroke=$x]
object a {
    object b
}
template(red)
template(1) a -- b");

            Assert.NotNull(document.Tokens);
            Assert.NotNull(document.Main);
            Assert.NotNull(document.ValidSyntax);
            Assert.NotNull(document.Root);
            Assert.NotNull(document.Diagram);

            Assert.Equal(5, document.Main!.Syntax.Declarations.Count(d => d.Value is not AST.InvalidDeclaration));
        }

        [Fact]
        public void CorrectTemplatedDocument()
        {
            var document = Parse(@"
class foo($x) {
    object $x
}
foo(""bar"")
");

            Assert.NotNull(document.Tokens);
            Assert.NotNull(document.Main);
            Assert.NotNull(document.ValidSyntax);
            Assert.NotNull(document.Root);
            Assert.NotNull(document.Diagram);
            
            Assert.Single(document.Root!.Nodes);
            Assert.Equal("bar", document.Root!.Nodes.Single().Region.Nodes.Single().Label?.Content);
        }

        [Fact]
        public void ReadUpToInvalidToken()
        {
            var document = Parse(@"
object a
!nonsense!");

            Assert.NotNull(document.Tokens);
            Assert.NotNull(document.Main);
            Assert.NotNull(document.ValidSyntax);
            Assert.NotNull(document.Root);
            Assert.NotNull(document.Diagram);

            Assert.Single(document.Main!.Syntax.Declarations, d => d.Value is not AST.EmptyDeclaration);
        }

        [Fact(Skip = "not yet implemented")]
        public void ReadAfterInvalidToken()
        {
            var document = Parse(@"
!nonsense!
object b");

            Assert.NotNull(document.Tokens);
            Assert.NotNull(document.Main);
            Assert.NotNull(document.ValidSyntax);
            Assert.NotNull(document.Root);
            Assert.NotNull(document.Diagram);

            Assert.Single(document.Main!.Syntax.Declarations);
        }

        [Fact]
        public void IgnoreBadDeclaration()
        {
            var document = Parse(
@"class foo                   // good
foo bar [fill=red]            // good
[why=an=attribute=here]       // bad
line bar -- bar [stroke=blue] // good");

            Assert.NotNull(document.Tokens);
            Assert.NotNull(document.Main);
            Assert.NotNull(document.ValidSyntax);
            Assert.NotNull(document.Root);
            Assert.NotNull(document.Diagram);

            Assert.Equal(3, document.Main!.Syntax.Declarations.Count(d => d.Value is not AST.InvalidDeclaration));
            Assert.Equal(3, document.ValidSyntax!.Declarations.Count());
        }

        [Fact]
        public void IgnoreBadDeclaration_SyntacticallyValid_MissingObject()
        {
            var document = Parse(
@"class foo                   // good
foo bar [fill=red]            // good
[why=an=attribute=here]       // bad
line bar -- baz [stroke=blue] // good");

            Assert.NotNull(document.Tokens);
            Assert.NotNull(document.Main);
            Assert.NotNull(document.ValidSyntax);
            Assert.NotNull(document.Root);
            Assert.NotNull(document.Diagram);

            Assert.Equal(3, document.Main!.Syntax.Declarations.Count(d => d.Value is not AST.InvalidDeclaration));
            Assert.Equal(3, document.ValidSyntax!.Declarations.Count());
        }

        [Fact]
        public void IgnoreBadDeclaration_SingleLine()
        {
            var document = Parse(
@"object a; []; object c { object }");

            Assert.NotNull(document.Tokens);
            Assert.NotNull(document.Main);
            Assert.NotNull(document.ValidSyntax);
            Assert.NotNull(document.Root);
            Assert.NotNull(document.Diagram);

            Assert.Equal(2, document.Main!.Syntax.Declarations.Count(d => d.Value is not AST.InvalidDeclaration));
        }

        [Fact]
        public void IgnoreBadDeclaration_NestInObject()
        {
            var document = Parse(
@"object {
    []
    object 
}");

            Assert.NotNull(document.Tokens);
            Assert.NotNull(document.Main);
            Assert.NotNull(document.ValidSyntax);
            Assert.NotNull(document.Root);
            Assert.NotNull(document.Diagram);

            Assert.Single(document.Root!.Nodes);
            Assert.Single(document.Root!.Nodes.Single().Region.Nodes);
        }

        [Fact]
        public void IgnoreBadDeclaration_NestInClass()
        {
            var document = Parse(
@"class a [align=start] {
    []    
}
a");

            Assert.NotNull(document.Tokens);
            Assert.NotNull(document.Main);
            Assert.NotNull(document.ValidSyntax);
            Assert.NotNull(document.Root);
            Assert.NotNull(document.Diagram);

            Assert.Equal(AlignmentKind.Start, document.Root!.Nodes.Single().Alignment.Columns);
        }

        [Fact]
        public void IgnoreBadDeclaration_NestInObjectInClass()
        {
            var document = Parse(
@"class foo {
    object {
        []
        object 
    }
}
foo");

            Assert.NotNull(document.Tokens);
            Assert.NotNull(document.Main);
            Assert.NotNull(document.ValidSyntax);
            Assert.NotNull(document.Root);
            Assert.NotNull(document.Diagram);

            Assert.Single(document.Root!.Nodes);
            Assert.Single(document.Root!.Nodes.Single().Region.Nodes);
            Assert.Single(document.Root!.Nodes.Single().Region.Nodes.Single().Region.Nodes);
        }

        [Fact]
        public void IgnoreBadDeclaration_IncompleteDocContent()
        {
            var document = Parse(
@"class foo [fill=red]
class bar [");

            Assert.NotNull(document.Tokens);
            Assert.NotNull(document.Main);
            Assert.NotNull(document.ValidSyntax);
            Assert.NotNull(document.Root);
            Assert.NotNull(document.Diagram);
        }

        [Fact]
        public void IgnoreBadDeclaration_IncompleteObjContent()
        {
            var document = Parse(
@"class foo [fill=red] {
    class bar [
}");

            Assert.NotNull(document.Tokens);
            Assert.NotNull(document.Main);
            Assert.NotNull(document.ValidSyntax);
            Assert.NotNull(document.Root);
            Assert.NotNull(document.Diagram);
        }
    }
}
