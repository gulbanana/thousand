using OmniSharp.Extensions.JsonRpc;
using System.Threading;
using System.Threading.Tasks;

namespace Thousand.LSP.Extensions
{
    class VFSHandler : IJsonRpcRequestHandler<VFSGet, string>
    {
        private readonly string stdlib;

        public VFSHandler()
        {
            stdlib = DiagramGenerator.ReadStdlib();
        }

        public Task<string> Handle(VFSGet request, CancellationToken cancellationToken)
        {
            return Task.FromResult(stdlib);
        }
    }
}
