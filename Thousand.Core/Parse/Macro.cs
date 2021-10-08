using Superpower.Model;
using System;
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

        public Range Range(int offset = 0)
        {
            var start = Location.Position - offset;
            var end = Remainder.Position - offset;
            return start..end;
        }
    }
}
