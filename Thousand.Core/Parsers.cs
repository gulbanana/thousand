using Superpower;
using Superpower.Model;
using Superpower.Parsers;
using System.Linq;

namespace Thousand
{
    public static class Parsers
    {
        public static TokenListParser<Kind, Unit> NewLine { get; } =
            Token.EqualTo(Kind.NewLine).Value(Unit.Value);

        public static TokenListParser<Kind, string> String { get; } =
            Token.EqualTo(Kind.String).Apply(TextParsers.String);

        public static TokenListParser<Kind, AST.Node> Node { get; } =
            Token.EqualToValue(Kind.Keyword, "node")
                 .IgnoreThen(String.Select(s => new AST.Node(s)));

        public static TokenListParser<Kind, AST.Node?> Declaration { get; } =
            Node.AsNullable().OptionalOrDefault();

        public static TokenListParser<Kind, AST.Document> Document { get; } =
            Declaration.ManyDelimitedBy(NewLine)
                .Select(ns => new AST.Document(ns.WhereNotNull().ToArray()))
                .AtEnd();
    }
}
