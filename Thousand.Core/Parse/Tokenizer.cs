﻿using Superpower;
using Superpower.Model;
using Superpower.Parsers;
using Superpower.Tokenizers;

namespace Thousand.Parse
{
    public static class Tokenizer
    {
        public static Tokenizer<TokenKind> Build()
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

            return new TokenizerBuilder<TokenKind>()
                .Ignore(parseInLineWhiteSpace)
                .Match(Character.EqualTo('['), TokenKind.LeftBracket)
                .Match(Character.EqualTo(']'), TokenKind.RightBracket)
                .Match(Character.EqualTo('='), TokenKind.EqualsSign)
                .Match(Character.EqualTo(','), TokenKind.Comma)
                .Match(parseNewLine, TokenKind.NewLine)
                .Match(Identifier.CStyle, TokenKind.Keyword)
                .Match(parseStringToken, TokenKind.String)
                .Match(Numerics.Integer, TokenKind.Integer)
                .Match(Character.EqualTo('#').IgnoreThen(Character.HexDigit.AtLeastOnce()), TokenKind.Colour)
                .Build();
        }
    }
}