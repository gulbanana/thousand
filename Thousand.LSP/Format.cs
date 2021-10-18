using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using System.Linq;
using System.Text;
using Thousand.Model;
using Thousand.Parse;

namespace Thousand.LSP
{
    static class Format
    {
        public static MarkedStringsOrMarkupContent CodeBlock(string content)
        {
            return new MarkedStringsOrMarkupContent(new MarkupContent { Kind = MarkupKind.Markdown, Value = "```\n" + content + "\n```" });
        }

        public static string Target(Identifier name)
        {
            var builder = new StringBuilder();

            var asIdentifier = TextParsers.Identifier(new Superpower.Model.TextSpan(name.Text));
            var plainText = asIdentifier.HasValue && asIdentifier.Remainder.IsAtEnd;

            if (!plainText) builder.Append('"');
            builder.Append(name.Text);
            if (!plainText) builder.Append('"');

            return builder.ToString();
        }

        public static string Canonicalise(AST.TolerantObject objekt)
        {
            var builder = new StringBuilder();

            builder.Append(Canonicalise(objekt.Classes.First().Value));
            foreach (var call in objekt.Classes.Skip(1))
            {
                builder.Append('.');
                builder.Append(Canonicalise(call.Value));
            }

            if (objekt.Name != null)
            {
                builder.Append(' ');
                builder.Append(Target(objekt.Name));
            }

            return builder.ToString();
        }

        public static string Canonicalise(AST.TolerantLine line)
        {
            var builder = new StringBuilder();

            builder.Append(Canonicalise(line.Classes.First().Value));
            foreach (var call in line.Classes.Skip(1))
            {
                builder.Append('.');
                builder.Append(Canonicalise(call.Value));
            }

            foreach (var segment in line.Segments)
            {
                builder.Append(' ');
                builder.Append(Target(segment.Target));

                if (segment.Direction.HasValue)
                {
                    builder.Append(' ');
                    builder.Append(segment.Direction switch
                    {
                        ArrowKind.Forward => "->",
                        ArrowKind.Backward => "<-",
                        ArrowKind.Neither => "--",
                        ArrowKind.Both => "<>"
                    });
                }
            }

            return builder.ToString();
        }

        public static string Canonicalise(AST.TolerantClass klass)
        {
            var builder = new StringBuilder();

            builder.Append("class ");
            builder.Append(klass.Name.Text);

            if (klass.Arguments.Value.Any())
            {
                builder.Append("($");
                builder.Append(klass.Arguments.Value.First().Name.Text);
                foreach (var arg in klass.Arguments.Value.Skip(1))
                {
                    builder.Append(", $");
                    builder.Append(arg.Name.Text);
                }
                builder.Append(')');
            }

            if (klass.BaseClasses.Any())
            {
                builder.Append(" : ");
                builder.Append(Canonicalise(klass.BaseClasses.First().Value));
                foreach (var call in klass.BaseClasses.Skip(1))
                {
                    builder.Append('.');
                    builder.Append(Canonicalise(call.Value));
                }
            }

            return builder.ToString();
        }

        public static string Canonicalise(AST.ClassCall call)
        {
            var builder = new StringBuilder();

            builder.Append(call.Name.Text);

            if (call.Arguments.Any())
            {
                builder.Append("(");
                builder.Append(call.Arguments.First().Span().ToStringValue());
                foreach (var arg in call.Arguments.Skip(1))
                {
                    builder.Append(", ");
                    builder.Append(arg.Span().ToStringValue());
                }
                builder.Append(")");
            }

            return builder.ToString();
        }
    }
}
