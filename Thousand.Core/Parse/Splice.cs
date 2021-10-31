using Superpower.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Thousand.Parse
{
    record Splice(Range Location, IReadOnlyList<Token<TokenKind>> Replacements)
    {
        public TokenList<TokenKind> Apply(TokenList<TokenKind> list)
        {
            return new TokenList<TokenKind>(Apply(list.ToArray()));
        }

        public Token<TokenKind>[] Apply(Token<TokenKind>[] list)
        {
            var newList = new Token<TokenKind>[list.Length - (Location.End.Value - Location.Start.Value) + Replacements.Count];

            Array.Copy(list, newList, Location.Start.Value);

            for (var i = 0; i < Replacements.Count; i++)
            {
                newList[Location.Start.Value + i] = Replacements[i];
            }
            
            Array.Copy(list, Location.End.Value, newList, Location.Start.Value + Replacements.Count, list.Length - Location.End.Value);

            return newList;
        }

        public override string ToString()
        {
            return $"Splice {Location} {Replacements.Dump()}";
        }
    }
}
