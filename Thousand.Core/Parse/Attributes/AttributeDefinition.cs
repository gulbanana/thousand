using Superpower;
using System;

namespace Thousand.Parse.Attributes
{
    public struct AttributeDefinition<T>
    {
        public string[] Names { get; }
        public TokenListParser<TokenKind, T> ValueParser { get; }

        public AttributeDefinition(string[] names, TokenListParser<TokenKind, T> valueParser)
        {
            Names = names;
            ValueParser = valueParser;
        }

        public AttributeDefinition(string name, TokenListParser<TokenKind, T> valueParser) : this(new[] { name }, valueParser) { }

        public AttributeDefinition<U> Select<U>(Func<T, U> f)
        {
            return new AttributeDefinition<U>(Names, ValueParser.Select(f));
        }
    }
}
