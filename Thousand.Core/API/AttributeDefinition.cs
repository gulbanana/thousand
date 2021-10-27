using Superpower;
using System;
using System.Linq;
using Thousand.Parse;

namespace Thousand.API
{
    public abstract class AttributeDefinition
    {
        public abstract string[] Names { get; }
        public abstract string? Documentation { get; }
    }

    public class AttributeDefinition<T> : AttributeDefinition
    {
        private static AttributeDefinition<T> Create<U, V>(string[] names, TokenListParser<TokenKind, U> valueParser, Func<U, V> selector, string? documentation) where V : T
        {
            return new AttributeDefinition<T>(names, valueParser.Select(value => (T)selector(value)), documentation);
        }

        internal static AttributeDefinition<T> Create<U, V>(string name, TokenListParser<TokenKind, U> valueParser, Func<U, V> selector) where V : T
        {
            return Create(new[] { name }, valueParser, selector, null);
        }

        internal static AttributeDefinition<T> Create<U, V>(string name, TokenListParser<TokenKind, U> valueParser, Func<U, V> selector, string description, string type, UseKind kind, params string[] examples) where V : T
        {
            return Create(new[] { name }, valueParser, selector, Format.Doc(description, type, kind, examples));
        }

        internal static AttributeDefinition<T> Create<U, V>(string name, AttributeType<U> type, Func<U, V> selector, string description, UseKind kind) where V : T
        {
            return Create(new[] { name }, type.Parser, selector, Format.Doc(description, type.Documentation, kind, type.Examples.Select(e => $"{name}={e}").ToArray()));
        }

        internal static AttributeDefinition<T> Create<U, V>(string name1, string name2, TokenListParser<TokenKind, U> valueParser, Func<U, V> selector) where V : T
        {
            return Create(new[] { name1, name2 }, valueParser, selector, null);
        }

        internal static AttributeDefinition<T> Create<U, V>(string name1, string name2, TokenListParser<TokenKind, U> valueParser, Func<U, V> selector, string description, string type, UseKind kind, params string[] examples) where V : T
        {
            return Create(new[] { name1, name2 }, valueParser, selector, Format.Doc(description, type, kind, examples));
        }

        internal static AttributeDefinition<T> Create<U, V>(string name1, string name2, AttributeType<U> type, Func<U, V> selector, string description, UseKind kind) where V : T
        {
            return Create(new[] { name1, name2 }, type.Parser, selector, Format.Doc(description, type.Documentation, kind, type.Examples.Select(e => $"{name1}={e}").ToArray()));
        }

        public override string[] Names { get; }
        public override string? Documentation { get; }
        public TokenListParser<TokenKind, T> ValueParser { get; }

        internal AttributeDefinition(string[] names, TokenListParser<TokenKind, T> valueParser, string? documentation = null)
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
