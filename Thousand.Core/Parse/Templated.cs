using Superpower.Model;
using System;

namespace Thousand.Parse
{
    public abstract record Templated : ITemplated
    {
        public TokenList<TokenKind> Location { get; set; }
        public TokenList<TokenKind> Remainder { get; set; }

        public Range Range(int offset = 0)
        {
            var start = Location.Position - offset;
            var end = Remainder.Position - offset;
            return start..end;
        }
    }
}
