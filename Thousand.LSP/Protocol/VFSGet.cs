using MediatR;
using OmniSharp.Extensions.JsonRpc;
using System.Threading.Tasks;

namespace Thousand.LSP.Protocol
{
    [Parallel, Method("thousand/vfsGet")]
    public class VFSGet : IRequest<string>
    {
        public string? Path { get; set; }
    }
}
