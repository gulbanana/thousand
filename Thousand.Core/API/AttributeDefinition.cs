using Superpower;
using System;
using System.Linq;
using Thousand.Parse;

namespace Thousand.API
{
    record AttributeGroup<T>(UseKind? Use)
    {
        public AttributeDefinition<T> Create<V>(string name, AttributeType<V> type, Func<V, T> selector, string description)
            => AttributeDefinition<T>.Create(new[] { name }, type, selector, description, Use);

        public AttributeDefinition<T> Create<V>(string name1, string name2, AttributeType<V> type, Func<V, T> selector, string description)
            => AttributeDefinition<T>.Create(new[] { name1, name2 }, type, selector, description, Use);
    }

    public abstract class AttributeDefinition
    {
        public abstract string[] Names { get; }
        public abstract string Documentation { get; }
    }

    public class AttributeDefinition<T> : AttributeDefinition
    {
        private static AttributeDefinition<T> Create<U, V>(string[] names, TokenListParser<TokenKind, U> valueParser, Func<U, V> selector, string documentation) where V : T
        {
            return new AttributeDefinition<T>(names, valueParser.Select(value => (T)selector(value)), documentation);
        }

        internal static AttributeDefinition<T> Create<U, V>(string[] names, AttributeType<U> type, Func<U, V> selector, string description, UseKind? kind) where V : T
        {
            return Create(names, type.Parser, selector, Format.Doc(description, type.Documentation, kind, type.Examples.Select(e => $"{names[0]}={e}").ToArray()));
        }

        public override string[] Names { get; }
        public override string Documentation { get; }
        public TokenListParser<TokenKind, T> ValueParser { get; }

        internal AttributeDefinition(string[] names, TokenListParser<TokenKind, T> valueParser, string documentation)
        {
            Names = names;
            ValueParser = valueParser;
            Documentation = documentation;
        }

        public AttributeDefinition<U> Select<U>(Func<T, U> f)
        {
            return new AttributeDefinition<U>(Names, ValueParser.Select(f), Documentation);
        }
    }
}
