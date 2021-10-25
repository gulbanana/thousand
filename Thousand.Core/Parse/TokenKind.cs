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

        [Token(Example = "|")]
        Pipe,

        [Token(Example = "=")]
        EqualsSign,

        [Token(Example = ",")]
        Comma,

        [Token(Example = ":")]
        Colon,

        [Token(Example = ".")]
        Period,

        [Token(Example = "$*")]
        Placeholder,

        LineSeparator,

        [Token(Category = "arrow")]
        LeftArrow,
        [Token(Category = "arrow")]
        RightArrow,
        [Token(Category = "arrow")]
        NoArrow,
        [Token(Category = "arrow")]
        DoubleArrow,

        [Token(Example = "none")]
        NoneKeyword,

        [Token(Example = "class")]
        ClassKeyword,
            
        Variable,

        Identifier,

        String,

        [Token(Description = "colour literal")]
        Colour,

        Number,
    }
}
