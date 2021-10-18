using MediatR;
using OmniSharp.Extensions.JsonRpc;
using OmniSharp.Extensions.LanguageServer.Protocol;
using System.Threading.Tasks;

namespace Thousand.LSP.Protocol
{
    [Parallel, Method("thousand/updatePreview")]
    public class UpdatePreview : IRequest
    {
        public DocumentUri? Uri { get; set; }
        public string? Filename { get; set; }
    }
}
