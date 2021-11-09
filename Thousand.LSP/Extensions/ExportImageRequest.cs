using MediatR;
using OmniSharp.Extensions.JsonRpc;
using OmniSharp.Extensions.LanguageServer.Protocol;
using System.Threading.Tasks;

namespace Thousand.LSP.Extensions
{
    [Parallel, Method("thousand/exportImage")]
    public class ExportImageRequest : IRequest<ExportImageResult>
    {
        public DocumentUri Uri { get; set; } = default!;
        public string Format { get; set; } = default!;
    }
}
