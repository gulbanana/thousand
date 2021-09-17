using Superpower.Display;

namespace Thousand
{
    public enum TokenKind
    {
        [Token(Example = "[")]
        LeftBracket,

        [Token(Example = "]")]
        RightBracket,

        [Token(Example = "=")]
        EqualsSign,

        [Token(Example = ",")]
        Comma,

        NewLine,

        Keyword,

        String,

        Colour,
    }
}
