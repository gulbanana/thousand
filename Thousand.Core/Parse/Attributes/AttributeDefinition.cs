using Superpower;
using System;

namespace Thousand.Parse.Attributes
{
    public abstract class AttributeDefinition
    {
        public abstract string[] Names { get; }
        public abstract string? Documentation { get; }
    }

    public class AttributeDefinition<T> : AttributeDefinition
    {
        public static AttributeDefinition<T> Create<U, V>(string[] names, TokenListParser<TokenKind, U> valueParser, Func<U, V> selector, string? documentation = null) where V : T
        {
            return new AttributeDefinition<T>(names, valueParser.Select(value => (T)selector(value)), documentation);
        }

        public static AttributeDefinition<T> Create<U, V>(string name, TokenListParser<TokenKind, U> valueParser, Func<U, V> selector, string? documentation = null) where V : T
        {
            return Create(new[] { name }, valueParser, selector, documentation);
        }

        public static AttributeDefinition<T> Create<U, V>(string name1, string name2, TokenListParser<TokenKind, U> valueParser, Func<U, V> selector, string? documentation = null) where V : T
        {
            return Create(new[] { name1, name2 }, valueParser, selector, documentation);
        }

        public override string[] Names { get; }
        public override string? Documentation { get; }
        public TokenListParser<TokenKind, T> ValueParser { get; }

        public AttributeDefinition(string[] names, TokenListParser<TokenKind, T> valueParser, string? documentation = null)
        {
            Names = names;
            ValueParser = valueParser;
            Documentation = documentation;
        }

        public AttributeDefinition(string name, TokenListParser<TokenKind, T> valueParser, string? documentation = null) : this(new[] { name }, valueParser, documentation) { }

        public AttributeDefinition<U> Select<U>(Func<T, U> f)
        {
            return new AttributeDefinition<U>(Names, ValueParser.Select(f), Documentation);
        }
    }
}
