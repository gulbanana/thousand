﻿using Superpower.Display;

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

        [Token(Example = "$*")]
        Placeholder,

        LineSeparator,

        [Token(Description = "arrow")]
        LeftArrow,
        [Token(Description = "arrow")]
        RightArrow,
        [Token(Description = "arrow")]
        NoArrow,
        [Token(Description = "arrow")]
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
