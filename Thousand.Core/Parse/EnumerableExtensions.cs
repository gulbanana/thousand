using Superpower;

namespace Thousand.Parse
{
    public static class ParserExtensions
    {
        public static TokenListParser<TokenKind, U> Cast<T, U>(this TokenListParser<TokenKind, T> pT) where T : U
        {
            return pT.Select(x => (U)x);
        }
    }
}
