using Superpower.Display;

namespace Thousand
{
    public enum TokenKind
    {
        NewLine,

        Keyword,

        String,

        [Token(Example = "[")]
        LeftBracket,

        [Token(Example = "]")]
        RightBracket,

        [Token(Example = "=")]
        EqualsSign,

        [Token(Example = ",")]
        Comma,
    }
}
