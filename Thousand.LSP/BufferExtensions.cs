using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Thousand.LSP
{
    static class BufferExtensions
    {
        public static Range AsRange(this Superpower.Model.TextSpan bufferSpan)
        {
            var bufferStart = bufferSpan.Position;
            var bufferEnd = bufferSpan.Skip(bufferSpan.Length).Position;
            return new(
                bufferStart.Line - 1,
                bufferStart.Column - 1,
                bufferEnd.Line - 1,
                bufferEnd.Column - 1
            );
        }
    }
}
