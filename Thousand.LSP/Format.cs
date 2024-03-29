﻿using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Thousand.Model;
using Thousand.Parse;

namespace Thousand.LSP
{
    static class Format
    {
        public static MarkupContent CodeBlock(string content)
        {
            return new MarkupContent { Kind = MarkupKind.Markdown, Value = "```\n" + content + "\n```" };
        }

        public static string Target(Name name)
        {
            var builder = new StringBuilder();

            var asIdentifier = TextParsers.Identifier(new Superpower.Model.TextSpan(name.AsKey));
            var plainText = asIdentifier.HasValue && asIdentifier.Remainder.IsAtEnd;

            if (!plainText) builder.Append('"');
            builder.Append(name.AsKey);
            if (!plainText) builder.Append('"');

            return builder.ToString();
        }

        public static string Canonicalise(AST.UntypedObject ast)
        {
            var builder = new StringBuilder();

            var classes = ast.Classes.Select(b => b.Value).WhereNotNull();
            builder.Append(Canonicalise(classes.First()));
            foreach (var call in classes.Skip(1))
            {
                builder.Append('.');
                builder.Append(Canonicalise(call));
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

        public static string Canonicalise(AST.UntypedLine ast)
        {
            var builder = new StringBuilder();

            var classes = ast.Classes.Select(b => b.Value).WhereNotNull();
            builder.Append(Canonicalise(classes.First()));
            foreach (var call in classes.Skip(1))
            {
                builder.Append('.');
                builder.Append(Canonicalise(call));
            }

            foreach (var segment in ast.Segments)
            {
                builder.Append(' ');
                if (segment.Target.IsT0)
                {
                    builder.Append(Target(segment.Target.AsT0));
                }
                else if (segment.Target.IsT1)
                {
                    builder.Append(Canonicalise(segment.Target.AsT1.Declaration.Value));
                }

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

        public static string Canonicalise(AST.UntypedClass ast)
        {
            var builder = new StringBuilder();

            builder.Append("class ");
            builder.Append(ast.Name.AsKey);

            if (ast.Arguments.Value.Any())
            {
                builder.Append('(');
                builder.Append('$');
                builder.Append(ast.Arguments.Value.First().Name.AsKey);
                foreach (var arg in ast.Arguments.Value.Skip(1))
                {
                    builder.Append(", $");
                    builder.Append(arg.Name.AsKey);
                }
                builder.Append(')');
            }

            var bases = ast.BaseClasses.Select(b => b.Value).WhereNotNull();
            if (bases.Any())
            {
                builder.Append(" : ");
                builder.Append(Canonicalise(bases.First()));
                foreach (var call in bases.Skip(1))
                {
                    builder.Append('.');
                    builder.Append(Canonicalise(call));
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

            builder.Append(call.Name.AsKey);

            if (call.Arguments.Any())
            {
                builder.Append('(');
                builder.Append(call.Arguments.First().SpanOrEmpty().ToStringValue());
                foreach (var arg in call.Arguments.Skip(1))
                {
                    builder.Append(", ");
                    builder.Append(arg.SpanOrEmpty().ToStringValue());
                }
                builder.Append(')');
            }

            return builder.ToString();
        }

        public static string Attributes(IReadOnlyList<AST.UntypedAttribute> attrs)
        {
            var validAttrs = attrs.Where(a => a.Key != null);
            var builder = new StringBuilder();

            builder.Append('[');

            var firstAttr = validAttrs.First();
            builder.Append(firstAttr.Key!.AsKey);
            builder.Append('=');
            builder.Append(firstAttr.Value.SpanOrEmpty().ToStringValue());

            foreach (var attr in validAttrs.Skip(1))
            {
                builder.Append(',');
                builder.Append(' ');
                builder.Append(attr.Key!.AsKey);
                builder.Append('=');
                builder.Append(attr.Value.SpanOrEmpty().ToStringValue());
            }

            builder.Append(']');

            return builder.ToString();
        }
    }
}
