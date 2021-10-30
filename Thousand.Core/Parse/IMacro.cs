using Superpower.Model;
using System;
using System.Collections.Generic;

namespace Thousand.Parse
{
    public interface IMacro
    {
        TokenList<TokenKind> Location { get; }
        TokenList<TokenKind> Remainder { get; }

        Range Range(int offset = 0);
        IEnumerable<Token<TokenKind>> Sequence();
        TextSpan Span(TextSpan endSpan);
        TextSpan SpanOrEmpty();
    }

    public interface IMacro<out T> : IMacro
    {
        T Value { get; }

        IMacro<U> Select<U>(Func<T, U> f);
    }
}