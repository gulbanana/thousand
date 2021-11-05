using MediatR;
using OmniSharp.Extensions.JsonRpc;
using OmniSharp.Extensions.LanguageServer.Protocol;
using System.Threading.Tasks;

namespace Thousand.LSP.Extensions
{
    [Parallel, Method("thousand/endPreview")]
    public class EndPreview : IRequest
    {
        public DocumentUri Uri { get; set; } = default!;
    }
}
