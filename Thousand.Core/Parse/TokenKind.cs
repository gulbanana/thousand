using Superpower.Display;

namespace Thousand.Parse
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

        [Token(Example = "<-")]
        LeftArrow,
        [Token(Example = "->")]
        RightArrow,

        [Token(Description = "colour literal")]
        Colour,

        Number,
    }
}
