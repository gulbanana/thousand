using Superpower;
using Superpower.Model;
using Superpower.Parsers;
using Superpower.Tokenizers;

namespace Thousand
{
    public static class Tokenizer
    {
        public static Tokenizer<Token> Build()
        {
            TextParser<Unit> parseNewLine = Character.EqualTo('\r').Optional().IgnoreThen(Character.EqualTo('\n').Value(Unit.Value));

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

            return new TokenizerBuilder<Token>()
                .Ignore(parseInLineWhiteSpace)
                .Match(parseNewLine, Token.NewLine)
                .Match(Identifier.CStyle, Token.Keyword)
                .Match(parseStringToken, Token.String)
                .Build();
        }
    }
}
