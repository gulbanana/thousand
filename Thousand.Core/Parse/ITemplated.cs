using Superpower.Model;

namespace Thousand.Parse
{
    internal interface ITemplated
    {
        TokenList<TokenKind> Location { get; set; }
        TokenList<TokenKind> Remainder { get; set; }
    }
}
