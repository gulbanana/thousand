using Superpower;

namespace Thousand
{
    public static class Parser
    {
        public static TokenListParser<Kind, AST.Document> Build()
        {
            return Parsers.Document;
        }
    }
}
