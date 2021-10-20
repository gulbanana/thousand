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

        public static string Canonicalise(AST.TolerantObject ast)
        {
            var builder = new StringBuilder();

            builder.Append(Canonicalise(ast.Classes.First().Value));
            foreach (var call in ast.Classes.Skip(1))
            {
                builder.Append('.');
                builder.Append(Canonicalise(call.Value));
            }

            if (ast.Name != null)
            {
                builder.Append(' ');
                builder.Append(Target(ast.Name));
            }

            if (ast.Attributes.Any())
            {
                builder.Append(' ');
                builder.Append(Attributes(ast.Attributes));
            }

            return builder.ToString();
        }

        public static string Canonicalise(AST.TolerantLine ast)
        {
            var builder = new StringBuilder();

            builder.Append(Canonicalise(ast.Classes.First().Value));
            foreach (var call in ast.Classes.Skip(1))
            {
                builder.Append('.');
                builder.Append(Canonicalise(call.Value));
            }

            foreach (var segment in ast.Segments)
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

            if (ast.Attributes.Any())
            {
                builder.Append(' ');
                builder.Append(Attributes(ast.Attributes));
            }

            return builder.ToString();
        }

        public static string Canonicalise(AST.TolerantClass ast)
        {
            var builder = new StringBuilder();

            builder.Append("class ");
            builder.Append(ast.Name.Text);

            if (ast.Arguments.Value.Any())
            {
                builder.Append('(');
                builder.Append('$');
                builder.Append(ast.Arguments.Value.First().Name.Text);
                foreach (var arg in ast.Arguments.Value.Skip(1))
                {
                    builder.Append(", $");
                    builder.Append(arg.Name.Text);
                }
                builder.Append(')');
            }

            if (ast.BaseClasses.Any())
            {
                builder.Append(" : ");
                builder.Append(Canonicalise(ast.BaseClasses.First().Value));
                foreach (var call in ast.BaseClasses.Skip(1))
                {
                    builder.Append('.');
                    builder.Append(Canonicalise(call.Value));
                }
            }

            if (ast.Attributes.Any())
            {
                builder.Append(' ');
                builder.Append(Attributes(ast.Attributes));
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

        public static string Attributes(AST.UntypedAttribute[] list)
        {
            var builder = new StringBuilder();

            builder.Append('[');

            var firstAttr = list.First();
            builder.Append(firstAttr.Key.Text);
            builder.Append('=');
            builder.Append(firstAttr.Value.Span().ToStringValue());

            foreach (var attr in list.Skip(1))
            {
                builder.Append(',');
                builder.Append(' ');
                builder.Append(attr.Key.Text);
                builder.Append('=');
                builder.Append(attr.Value.Span().ToStringValue());
            }

            builder.Append(']');

            return builder.ToString();
        }
    }
}
