using Superpower;
using Superpower.Parsers;

namespace Thousand.Parse
{
    internal static class ParserExtensions
    {
        public static TokenListParser<TokenKind, U> Cast<T, U>(this TokenListParser<TokenKind, T> pT) where T : U
        {
            return pT.Select(x => (U)x);
        }

        public static TokenListParser<TokenKind, T?> OrNone<T>(this TokenListParser<TokenKind, T> pT) where T : struct
        {
            return pT
                .Select(v => new T?(v))
                .Or(Token.EqualTo(TokenKind.NoneKeyword).Value(new T?()));
        }
    }
}
