using Microsoft.Extensions.Logging.Abstractions;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using System.Linq;
using System.Threading;
using Thousand.LSP;
using Thousand.LSP.Analyse;
using Thousand.LSP.Handlers;
using Xunit;

namespace Thousand.Tests.LSP
{
    public class Completions
    {
        private readonly IDiagnosticService diagnostics;
        private readonly BufferService buffers;
        private readonly AnalysisService semantics;
        private readonly CompletionHandler handler;

        public Completions()
        {
            var api = new API.Metadata();

            diagnostics = new MockDiagnosticService();
            buffers = new BufferService();
            semantics = new AnalysisService(NullLogger<AnalysisService>.Instance, api, buffers, diagnostics, new MockGenerationService());
            handler = new CompletionHandler(NullLogger<CompletionHandler>.Instance, api, semantics);
        }

        private Analysis Parse(string source)
        {
            var key = DocumentUri.From("file://test.1000");
            buffers.Add(key, source);
            return semantics.Analyse(new ServerOptions { NoStandardLibrary = true }, key, CancellationToken.None);
        }

        [Fact]
        public void FindAttribute()
        {
            var analysis = Parse(@"object []");
            var list = handler.GenerateCompletions(analysis, new Position(0, 8));
            Assert.NotEmpty(list.Items);
            Assert.Contains(list.Items, item => item.Label == "shape");
        }

        [Fact]
        public void FindAttribute_LastToken()
        {
            var analysis = Parse(@"object [");
            var list = handler.GenerateCompletions(analysis, new Position(0, 8));
            Assert.NotEmpty(list.Items);
            Assert.Contains(list.Items, item => item.Label == "shape");
        }

        [Fact]
        public void FindAttribute_Existing()
        {
            var analysis = Parse(@"object [stroke=none]");
            var list = handler.GenerateCompletions(analysis, new Position(0, 14));
            Assert.NotEmpty(list.Items);
            Assert.Contains(list.Items, item => item.Label == "shape");
        }

        [Fact]
        public void FindAttribute_AfterExisting()
        {
            var analysis = Parse(@"object [stroke=none,]");
            var list = handler.GenerateCompletions(analysis, new Position(0, 20));
            Assert.NotEmpty(list.Items);
            Assert.Contains(list.Items, item => item.Label == "shape");
        }

        [Fact]
        public void FindAttribute_AfterEmpty()
        {
            var analysis = Parse(@"object [stroke,]");
            var list = handler.GenerateCompletions(analysis, new Position(0, 15));
            Assert.NotEmpty(list.Items);
            Assert.Contains(list.Items, item => item.Label == "shape");
        }

        [Fact]
        public void FindKeyword()
        {
            var analysis = Parse(@"");
            var list = handler.GenerateCompletions(analysis, new Position(0, 0));
            Assert.Single(list.Items);
            Assert.Equal("class", list.Items.Single().Label);
        }

        [Fact]
        public void FindKeyword_Partial()
        {
            var analysis = Parse(@"cl");
            var list = handler.GenerateCompletions(analysis, new Position(0, 0));
            Assert.Single(list.Items);
            Assert.Equal("class", list.Items.Single().Label);
        }

        [Fact]
        public void FindClass()
        {
            var analysis = Parse(@"class object
obj");
            var list = handler.GenerateCompletions(analysis, new Position(1, 3));
            Assert.Equal(2, list.Items.Count());
            Assert.Contains(list.Items, i => i.Label == "object");
        }

        [Fact]
        public void FindClass_Multiple()
        {
            var analysis = Parse(@"class a; class b
obj");
            var list = handler.GenerateCompletions(analysis, new Position(1, 3));
            Assert.Equal(3, list.Items.Count());
            Assert.Contains(list.Items, i => i.Label == "a");
            Assert.Contains(list.Items, i => i.Label == "b");
        }

        [Fact]
        public void FindClass_OuterScope()
        {
            var analysis = Parse(@"class object
group {
    obj
}");
            var list = handler.GenerateCompletions(analysis, new Position(2, 7));
            Assert.Equal(2, list.Items.Count());
            Assert.Contains(list.Items, i => i.Label == "object");
        }

        [Fact]
        public void FindClass_InnerScope()
        {
            var analysis = Parse(@"group {
    class object
}
obj");
            var list = handler.GenerateCompletions(analysis, new Position(3, 3));
            Assert.Single(list.Items);
        }

        [Fact]
        public void FindClass_SameScope()
        {
            var analysis = Parse(@"group {
    class object
    obj
}");
            var list = handler.GenerateCompletions(analysis, new Position(2, 7));
            Assert.Equal(2, list.Items.Count());
            Assert.Contains(list.Items, i => i.Label == "object");
        }

        [Fact]
        public void FindClass_SameScope_Unprompted()
        {
            var analysis = Parse(@"group {
    class object
    
}");
            var list = handler.GenerateCompletions(analysis, new Position(2, 4));
            Assert.Equal(2, list.Items.Count());
            Assert.Contains(list.Items, i => i.Label == "object");
        }

        [Fact]
        public void FindClass_Unprompted()
        {
            var analysis = Parse(@"class object

");
            var list = handler.GenerateCompletions(analysis, new Position(1, 0));
            Assert.Equal(2, list.Items.Count());
            Assert.Contains(list.Items, i => i.Label == "object");
        }

        [Fact]
        public void FindClass_Unprompted_LastLine()
        {
            var analysis = Parse(@"class object
");
            var list = handler.GenerateCompletions(analysis, new Position(1, 0));
            Assert.Equal(2, list.Items.Count());
            Assert.Contains(list.Items, i => i.Label == "object");
        }

        [Fact]
        public void FindClass_Unprompted_SingleLine()
        {
            var analysis = Parse(@"class object;");
            var list = handler.GenerateCompletions(analysis, new Position(0, 13));
            Assert.Equal(2, list.Items.Count());
            Assert.Contains(list.Items, i => i.Label == "object");
        }

        [Fact]
        public void FindClass_Unprompted_WithinLine()
        {
            var analysis = Parse(@"class object;;");
            var list = handler.GenerateCompletions(analysis, new Position(0, 13));
            Assert.Equal(2, list.Items.Count());
            Assert.Contains(list.Items, i => i.Label == "object");
        }

        [Fact]
        public void FindClass_Mixin()
        {
            var analysis = Parse(@"class foo
foo.
");
            var list = handler.GenerateCompletions(analysis, new Position(1, 4));
            Assert.Single(list.Items);
            Assert.Equal("foo", list.Items.Single().Label);
        }

        [Fact]
        public void Dont_FindClass_ForObjectName()
        {
            var analysis = Parse(@"class foo
foo f
");
            var list = handler.GenerateCompletions(analysis, new Position(1, 5));
            Assert.Empty(list.Items);
        }
    }
}
