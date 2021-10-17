using Microsoft.Extensions.Logging.Abstractions;
using OmniSharp.Extensions.LanguageServer.Protocol;
using System.Linq;
using System.Threading.Tasks;
using Thousand.LSP;
using Thousand.Model;
using Xunit;

namespace Thousand.Tests.LSP
{
    public class ErrorToleration
    {
        private readonly BufferService buffers;
        private readonly SemanticService semantics;

        public ErrorToleration()
        {
            buffers = new BufferService();
            semantics = new SemanticService(NullLogger<SemanticService>.Instance, buffers, new MockDiagnosticService());
        }

        private Task<SemanticDocument> ParseAsync(string source)
        {
            var key = DocumentUri.From("file://test.1000");
            buffers.Add(key, source);
            semantics.Reparse(key);
            return semantics.GetParseAsync(key);
        }

        [Fact]
        public async Task CorrectSimpleDocument()
        {
            var document = await ParseAsync(
@"class foo
foo bar [fill=red]
foo baz [stroke=red]
line bar -- baz [stroke=blue]");

            Assert.NotNull(document.Tokens);
            Assert.NotNull(document.Syntax);
            Assert.NotNull(document.ValidSyntax);
            Assert.NotNull(document.Rules);
            Assert.NotNull(document.Diagram);

            Assert.Equal(4, document.Syntax!.Declarations.Count(d => !d.Value.IsT0));
        }

        [Fact]
        public async Task CorrectComplexDocument()
        {
            var document = await ParseAsync(
@"class object
class template($x) [stroke=$x]
object a {
    object b
}
template(red)
template(1) a -- b");

            Assert.NotNull(document.Tokens);
            Assert.NotNull(document.Syntax);
            Assert.NotNull(document.ValidSyntax);
            Assert.NotNull(document.Rules);
            Assert.NotNull(document.Diagram);

            Assert.Equal(5, document.Syntax!.Declarations.Count(d => !d.Value.IsT0));
        }

        [Fact]
        public async Task ReadUpToInvalidToken()
        {
            var document = await ParseAsync(@"
object a
!nonsense!");

            Assert.NotNull(document.Tokens);
            Assert.NotNull(document.Syntax);
            Assert.NotNull(document.ValidSyntax);
            Assert.NotNull(document.Rules);
            Assert.NotNull(document.Diagram);

            Assert.Single(document.Syntax!.Declarations);
        }

        [Fact(Skip = "not yet implemented")]
        public async Task ReadAfterInvalidToken()
        {
            var document = await ParseAsync(@"
!nonsense!
object b");

            Assert.NotNull(document.Tokens);
            Assert.NotNull(document.Syntax);
            Assert.NotNull(document.ValidSyntax);
            Assert.NotNull(document.Rules);
            Assert.NotNull(document.Diagram);

            Assert.Single(document.Syntax!.Declarations);
        }

        [Fact]
        public async Task IgnoreBadDeclaration()
        {
            var document = await ParseAsync(
@"class foo                   // good
foo bar [fill=red]            // good
[why=an=attribute=here]       // bad
line bar -- bar [stroke=blue] // good");

            Assert.NotNull(document.Tokens);
            Assert.NotNull(document.Syntax);
            Assert.NotNull(document.ValidSyntax);
            Assert.NotNull(document.Rules);
            Assert.NotNull(document.Diagram);

            Assert.Equal(3, document.Syntax!.Declarations.Count(d => !d.Value.IsT0));
            Assert.Equal(3, document.ValidSyntax!.Declarations.Count());
        }

        [Fact]
        public async Task IgnoreBadDeclaration_SyntacticallyValid_MissingObject()
        {
            var document = await ParseAsync(
@"class foo                   // good
foo bar [fill=red]            // good
[why=an=attribute=here]       // bad
line bar -- baz [stroke=blue] // good");

            Assert.NotNull(document.Tokens);
            Assert.NotNull(document.Syntax);
            Assert.NotNull(document.ValidSyntax);
            Assert.NotNull(document.Rules);
            Assert.NotNull(document.Diagram);

            Assert.Equal(3, document.Syntax!.Declarations.Count(d => !d.Value.IsT0));
            Assert.Equal(3, document.ValidSyntax!.Declarations.Count());
        }

        [Fact]
        public async Task IgnoreBadDeclaration_SingleLine()
        {
            var document = await ParseAsync(
@"object a; []; object c { object }");

            Assert.NotNull(document.Tokens);
            Assert.NotNull(document.Syntax);
            Assert.NotNull(document.ValidSyntax);
            Assert.NotNull(document.Rules);
            Assert.NotNull(document.Diagram);

            Assert.Equal(2, document.Syntax!.Declarations.Count(d => !d.Value.IsT0));
        }

        [Fact]
        public async Task IgnoreBadDeclaration_NestInObject()
        {
            var document = await ParseAsync(
@"object {
    []
    object 
}");

            Assert.NotNull(document.Tokens);
            Assert.NotNull(document.Syntax);
            Assert.NotNull(document.ValidSyntax);
            Assert.NotNull(document.Rules);
            Assert.NotNull(document.Diagram);

            Assert.Single(document.Rules!.Region.Objects);
            Assert.Single(document.Rules!.Region.Objects.Single().Region.Objects);
        }

        [Fact(Skip = "not yet implemented")]
        public async Task IgnoreBadDeclaration_NestInClass()
        {
            var document = await ParseAsync(
@"class a [align=start] {
    []    
}
a");

            Assert.NotNull(document.Tokens);
            Assert.NotNull(document.Syntax);
            Assert.NotNull(document.ValidSyntax);
            Assert.NotNull(document.Rules);
            Assert.NotNull(document.Diagram);

            Assert.Equal(AlignmentKind.Start, document.Rules!.Region.Objects.Single().Alignment.Columns);
        }
    }
}
