using Superpower.Display;

namespace Thousand.Parse
{
    public enum TokenKind
    {
        [Token(Example = "[")]
        LeftBracket,

        [Token(Example = "]")]
        RightBracket,

        [Token(Example = "{")]
        LeftBrace,

        [Token(Example = "}")]
        RightBrace,

        [Token(Example = "(")]
        LeftParenthesis,

        [Token(Example = ")")]
        RightParenthesis,

        [Token(Example = "=")]
        EqualsSign,

        [Token(Example = ",")]
        Comma,

        [Token(Example = ":")]
        Colon,

        [Token(Example = ".")]
        Period,

        NewLine,

        [Token(Example = "none")]
        NoneKeyword,

        [Token(Example = "class")]
        ClassKeyword,

        Identifier,

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
