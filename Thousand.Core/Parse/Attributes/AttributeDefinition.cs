using Superpower;
using System;

namespace Thousand.Parse.Attributes
{
    public struct AttributeDefinition<T>
    {
        public static AttributeDefinition<T> Create<U, V>(string[] names, TokenListParser<TokenKind, U> valueParser, Func<U, V> selector) where V : T
        {
            return new AttributeDefinition<T>(names, valueParser.Select(value => (T)selector(value)));
        }

        public static AttributeDefinition<T> Create<U, V>(string name, TokenListParser<TokenKind, U> valueParser, Func<U, V> selector) where V : T
        {
            return Create(new[] { name }, valueParser, selector);
        }

        public static AttributeDefinition<T> Create<U, V>(string name1, string name2, TokenListParser<TokenKind, U> valueParser, Func<U, V> selector) where V : T
        {
            return Create(new[] { name1, name2 }, valueParser, selector);
        }

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
