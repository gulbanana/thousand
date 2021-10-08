using Superpower.Model;

namespace Thousand.Parse
{
    public abstract record Template : ITemplated
    {
        public TokenList<TokenKind> Location { get; set; }
        public TokenList<TokenKind> Remainder { get; set; }
    }
}
