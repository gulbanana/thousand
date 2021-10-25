using Superpower;
using Superpower.Model;
using Superpower.Parsers;
using Superpower.Tokenizers;

namespace Thousand.Parse
{
    public static class Tokenizer
    {
        public static Tokenizer<TokenKind> Build()
        {
            TextParser<Unit> parseLineSeparator = Character.EqualTo(';').Or(Character.EqualTo('\r').Optional().IgnoreThen(Character.EqualTo('\n'))).Value(Unit.Value);

            TextParser<Unit> parseInLineWhiteSpace = input =>
            {
                var next = input.ConsumeChar();
                while (next.HasValue && char.IsWhiteSpace(next.Value) && next.Value != '\r' && next.Value != '\n')
                {
                    next = next.Remainder.ConsumeChar();
                }

                return next.Location == input ?
                    Result.Empty<Unit>(input) :
                    Result.Value(Unit.Value, input, next.Location);
            };

            TextParser<Unit> parseStringToken =
                from open in Character.EqualTo('"')
                from content in Character.EqualTo('\\').IgnoreThen(Character.AnyChar).Value(Unit.Value).Try()
                    .Or(Character.Except('"').Value(Unit.Value))
                    .IgnoreMany()
                from close in Character.EqualTo('"')
                select Unit.Value;

            return new TokenizerBuilder<TokenKind>()
                .Ignore(parseInLineWhiteSpace)
                .Ignore(Comment.CPlusPlusStyle)
                .Match(parseLineSeparator, TokenKind.LineSeparator)
                .Match(Character.EqualTo('['), TokenKind.LeftBracket)
                .Match(Character.EqualTo(']'), TokenKind.RightBracket)
                .Match(Character.EqualTo('{'), TokenKind.LeftBrace)
                .Match(Character.EqualTo('}'), TokenKind.RightBrace)
                .Match(Character.EqualTo('('), TokenKind.LeftParenthesis)
                .Match(Character.EqualTo(')'), TokenKind.RightParenthesis)
                .Match(Character.EqualTo('|'), TokenKind.Pipe)
                .Match(Character.EqualTo('='), TokenKind.EqualsSign)
                .Match(Character.EqualTo(','), TokenKind.Comma)
                .Match(Character.EqualTo(':'), TokenKind.Colon)
                .Match(Character.EqualTo('.'), TokenKind.Period)
                .Match(Span.EqualTo("$*"), TokenKind.Placeholder)
                .Match(Span.EqualToIgnoreCase("none"), TokenKind.NoneKeyword)
                .Match(Span.EqualToIgnoreCase("class"), TokenKind.ClassKeyword)
                .Match(Character.EqualTo('#').IgnoreThen(Character.HexDigit.AtLeastOnce()), TokenKind.Colour)
                .Match(Character.EqualTo('<').IgnoreThen(Character.EqualTo('-').Many()).IgnoreThen(Character.EqualTo('>')), TokenKind.DoubleArrow)
                .Match(Character.EqualTo('<').IgnoreThen(Character.EqualTo('-').AtLeastOnce()), TokenKind.LeftArrow)
                .Match(Character.EqualTo('-').AtLeastOnce().IgnoreThen(Character.EqualTo('>')), TokenKind.RightArrow)
                .Match(Character.EqualTo('-').IgnoreThen(Character.EqualTo('-').AtLeastOnce()), TokenKind.NoArrow)
                .Match(Numerics.Decimal, TokenKind.Number)
                .Match(parseStringToken, TokenKind.String)
                .Match(Character.EqualTo('$').IgnoreThen(TextParsers.Identifier), TokenKind.Variable)
                .Match(Span.MatchedBy(TextParsers.Identifier), TokenKind.Identifier)
                .Build();
        }
    }
}
