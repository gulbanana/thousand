using Superpower;

namespace Thousand
{
    public static class Parser
    {
        public static TokenListParser<TokenKind, AST.Document> Build()
        {
            return Parsers.Document;
        }
    }
}
