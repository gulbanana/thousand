using Superpower;
using Superpower.Display;
using Superpower.Model;
using Superpower.Parsers;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Thousand.Parse
{
    public class Tokenizer : Tokenizer<TokenKind>
    {
        private readonly TokenKind[] singles = new TokenKind[128];
        private readonly List<(TextParser<Unit> parser, TokenKind kind)> matchers = new()
        {
            (Character.EqualTo('#').IgnoreThen(Character.HexDigit.AtLeastOnce()).Value(Unit.Value), TokenKind.Colour),
            (Span.EqualToIgnoreCase("none").Value(Unit.Value), TokenKind.NoneKeyword),
            (Span.EqualToIgnoreCase("class").Value(Unit.Value), TokenKind.ClassKeyword),
            (Numerics.Decimal.Value(Unit.Value), TokenKind.Number),
            (Character.EqualTo('$').IgnoreThen(TextParsers.Identifier).Value(Unit.Value), TokenKind.Variable),
            (TextParsers.Identifier.Value(Unit.Value), TokenKind.Identifier)
        };

        public Tokenizer()
        {
            singles['['] = TokenKind.LeftBracket;
            singles[']'] = TokenKind.RightBracket;
            singles['{'] = TokenKind.LeftBrace;
            singles['}'] = TokenKind.RightBrace;
            singles['('] = TokenKind.LeftParenthesisUnbound;
            singles[')'] = TokenKind.RightParenthesis;
            singles['='] = TokenKind.EqualsSign;
            singles[','] = TokenKind.Comma;
            singles[':'] = TokenKind.Colon;
            singles['.'] = TokenKind.Period;
        }
        
        protected override IEnumerable<Result<TokenKind>> Tokenize(TextSpan span, TokenizationState<TokenKind> state)
        {
            var next = SkipWhitespaceAndComments(span);
            if (!next.HasValue)
            {
                yield break;
            }

            do
            {
                if (next.Value == '(' && state.Previous.HasValue && state.Previous.Value.Position.Absolute + state.Previous.Value.Span.Length == next.Location.Position.Absolute)
                {
                    yield return Result.Value(TokenKind.LeftParenthesisBound, next.Location, next.Remainder);
                    next = SkipWhitespaceAndComments(next.Remainder);
                }
                else if (next.Value < singles.Length && singles[next.Value] != TokenKind.None)
                {
                    yield return Result.Value(singles[next.Value], next.Location, next.Remainder);
                    next = SkipWhitespaceAndComments(next.Remainder);
                }
                else if (next.Value == ';' || next.Value == '\r' || next.Value == '\n')
                {
                    var remainder = next.Value == '\r' ? next.Remainder.Skip(1) : next.Remainder;
                    yield return Result.Value(TokenKind.LineSeparator, next.Location, remainder);
                    next = SkipWhitespaceAndComments(remainder);
                }
                else if (next.Value == '"')
                {
                    var inString = next.Remainder.ConsumeChar();
                    var lastChar = '"';
                    while (inString.HasValue && (lastChar == '\\' || inString.Value != '"'))
                    {
                        lastChar = inString.Value;
                        inString = inString.Remainder.ConsumeChar();
                    }
                    if (inString.HasValue && inString.Value == '"')
                    {
                        yield return Result.Value(TokenKind.String, next.Location, inString.Remainder);
                        next = SkipWhitespaceAndComments(inString.Remainder);
                    }
                    else
                    {
                        yield return Result.Empty<TokenKind>(next.Location, "incomplete string, expected `\"`");
                    }
                }
                else if ((next.Value == '<' || next.Value == '-') && Arrow(next, out var remainder))
                {
                    yield return Result.Value(TokenKind.Arrow, next.Location, remainder);
                    next = SkipWhitespaceAndComments(remainder);
                }
                else
                {
                    var succeeded = false;
                    foreach (var matcher in matchers)
                    {
                        var attempt = matcher.parser(next.Location);
                        if (attempt.HasValue)
                        {
                            yield return Result.Value(matcher.kind, attempt.Location, attempt.Remainder);
                            next = SkipWhitespaceAndComments(attempt.Remainder);
                            succeeded = true;
                            break;
                        }
                    }

                    if (!succeeded)
                    {
                        var failure = Result.Empty<TokenKind>(next.Location);
                        foreach (var matcher in matchers)
                        {
                            var attempt = matcher.parser(next.Location);
                            if (!attempt.HasValue && attempt.ErrorPosition.Absolute > failure.ErrorPosition.Absolute)
                            {
                                var problem = attempt.Remainder.IsAtEnd ? "incomplete" : "invalid";
                                var augmentedMessage = $"{problem} {FormatExpectation(matcher.kind)}, {attempt.FormatErrorMessageFragment()}";
                                if (!attempt.Remainder.IsAtEnd)
                                    augmentedMessage += $" at line {attempt.Remainder.Position.Line}, column {attempt.Remainder.Position.Column}";
                                failure = Result.Empty<TokenKind>(next.Location, augmentedMessage);
                            }
                        }
                        yield return failure;
                    }
                }
            } while (next.HasValue);
        }

        private static Result<char> SkipWhitespaceAndComments(TextSpan span)
        {
            var next = span.ConsumeChar();

            while (next.HasValue && char.IsWhiteSpace(next.Value) && next.Value != '\r' && next.Value != '\n')
            {
                next = next.Remainder.ConsumeChar();
            }

            if (next.HasValue && next.Value == '/')
            {
                var second = next.Remainder.ConsumeChar();
                if (second.HasValue && second.Value == '/')
                {
                    next = second.Remainder.ConsumeChar();

                    while (next.HasValue && next.Value != '\n')
                    {
                        next = next.Remainder.ConsumeChar();
                    }
                }
            }

            return next;
        }

        private static bool Arrow(Result<char> first, out TextSpan remainder)
        {
            var second = first.Remainder.ConsumeChar();
            if (second.HasValue && (second.Value == '>' || second.Value == '-'))
            {
                remainder = second.Remainder;
                return true;
            }

            remainder = TextSpan.None;
            return false;
        }

        #region "Generic formatting code from Superpower"
        static TokenAttribute? TryGetTokenAttribute(Type type)
        {
            return type.GetTypeInfo().GetCustomAttribute<TokenAttribute>();
        }

        static TokenAttribute? TryGetTokenAttribute(TokenKind kind)
        {
            var kindTypeInfo = typeof(TokenKind).GetTypeInfo();
            var field = kindTypeInfo.GetDeclaredField(kind!.ToString()!);
            if (field != null)
            {
                return field.GetCustomAttribute<TokenAttribute>() ?? TryGetTokenAttribute(typeof(TokenKind));
            }

            return TryGetTokenAttribute(typeof(TokenKind));
        }

        static string FormatExpectation(TokenKind kind)
        {
            var description = TryGetTokenAttribute(kind);
            if (description != null)
            {
                if (description.Description != null)
                    return description.Description;
                if (description.Example != null)
                    return "`" + description.Example + "`";
            }

            return kind.ToString().ToLower();
        }
        #endregion
    }
}
