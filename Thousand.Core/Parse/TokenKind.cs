using Superpower.Display;

namespace Thousand.Parse
{
    public enum TokenKind
    {
        None = 0,

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

        LineSeparator,

        Arrow,

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
