using Superpower;
using Superpower.Model;
using Superpower.Parsers;
using Superpower.Tokenizers;
using System;

namespace Thousand
{
    public enum Token
    {
        NewLine,
        Keyword,
        String
    }

    public static class Tokenizers
    {
        public static Tokenizer<Token> Thousand { get; } = new TokenizerBuilder<Token>()
            .Ignore(Parsers.InlineWhiteSpace)
            .Match(Span.EqualTo(Environment.NewLine), Token.NewLine)
            .Match(Identifier.CStyle, Token.Keyword)
            .Match(Parsers.StringToken, Token.String)
            .Build();
    }

    public static class Parsers
    {
        public static TextParser<TextSpan> InlineWhiteSpace { get; } = input =>
        {
            var next = input.ConsumeChar();
            while (next.HasValue && char.IsWhiteSpace(next.Value) && next.Value != '\r' && next.Value != '\n')
            {
                next = next.Remainder.ConsumeChar();
            }

            return next.Location == input ?
                Result.Empty<TextSpan>(input) :
                Result.Value(input.Until(next.Location), input, next.Location);
        };

        public static TextParser<Unit> StringToken { get; } =
            from open in Character.EqualTo('"')
            from content in Character.EqualTo('\\').IgnoreThen(Character.AnyChar).Value(Unit.Value).Try()
                .Or(Character.Except('"').Value(Unit.Value))
                .IgnoreMany()
            from close in Character.EqualTo('"')
            select Unit.Value;
    }
}
