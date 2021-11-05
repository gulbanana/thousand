using MediatR;
using OmniSharp.Extensions.JsonRpc;
using OmniSharp.Extensions.LanguageServer.Protocol;
using System.Threading.Tasks;

namespace Thousand.LSP.Extensions
{
    [Parallel, Method("thousand/beginPreview")]
    public class BeginPreview : IRequest
    {
        public DocumentUri Uri { get; set; } = default!;
    }
}
