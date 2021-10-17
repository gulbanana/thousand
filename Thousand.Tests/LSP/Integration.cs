using OmniSharp.Extensions.LanguageServer.Protocol;
using System.Linq;
using System.Threading.Tasks;
using Thousand.LSP;
using Xunit;

namespace Thousand.Tests.LSP
{
    public class Integration
    {
        private readonly BufferService buffers;
        private readonly SemanticService semantics;

        public Integration()
        {
            buffers = new BufferService();
            semantics = new SemanticService(buffers);
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

            Assert.Equal(5, document.Syntax!.Declarations.Count(d => !d.Value.IsT0));
        }

        [Fact]
        public async Task PartiallyIncorrectDocument()
        {
            var document = await ParseAsync(
@"class foo                   // good
foo bar [fill=red]            // good
[why=an=attribute=here]       // bad
line bar -- baz [stroke=blue] // good");

            Assert.NotNull(document.Tokens);
            Assert.NotNull(document.Syntax);

            Assert.Equal(3, document.Syntax!.Declarations.Count(d => !d.Value.IsT0));
        }

        [Fact]
        public async Task PartiallyIncorrectComplexDocument()
        {
            var document = await ParseAsync(
@"object a; []; object c { object }");

            Assert.NotNull(document.Tokens);
            Assert.NotNull(document.Syntax);

            Assert.Equal(2, document.Syntax!.Declarations.Count(d => !d.Value.IsT0));
        }

        [Fact]
        public async Task AlmostEntirelyIncorrectDocument()
        {
            var document = await ParseAsync(@"
nonsense!


class   foo  ($b,  $c)  : bar   .baz($c) [attr=val]



[garbage]");

            Assert.NotNull(document.Tokens);
            Assert.NotNull(document.Syntax);

            Assert.Equal(1, document.Syntax!.Declarations.Count(d => d.Value.IsT2));
        }
    }
}
