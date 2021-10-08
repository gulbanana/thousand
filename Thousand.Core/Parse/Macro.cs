using Superpower.Model;
using System.Collections.Generic;
using System.Linq;

namespace Thousand.Parse
{
    public record Macro(TokenList<TokenKind> Location, TokenList<TokenKind> Remainder)
    {
        public IEnumerable<Token<TokenKind>> Sequence()
        {
            return Location.Take(Remainder.Position - Location.Position);
        }
    }
}
