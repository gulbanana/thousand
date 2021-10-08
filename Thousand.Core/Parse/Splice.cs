using Superpower.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Thousand.Parse
{
    internal record Splice(Range Location, Token<TokenKind>[] Replacements)
    {
        public TokenList<TokenKind> Apply(TokenList<TokenKind> list)
        {
            var newList = new List<Token<TokenKind>>();

            newList.AddRange(list.Take(Location.Start.Value));
            newList.AddRange(Replacements);
            newList.AddRange(list.Skip(Location.End.Value));

            return new(newList.ToArray());
        }
    }
}
