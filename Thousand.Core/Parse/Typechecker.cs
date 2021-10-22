using Superpower;
using Superpower.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Token = Superpower.Model.Token<Thousand.Parse.TokenKind>;
using TokenList = Superpower.Model.TokenList<Thousand.Parse.TokenKind>;

namespace Thousand.Parse
{
    // Invariant: accepts only untyped AST which has already been preprocessed. Will fail hard on unpreprocessed input!
    public class Typechecker
    {
        public static bool TryTypecheck(Attributes.Metadata metadata, GenerationState state, AST.UntypedDocument inputAST, bool allowErrors, [NotNullWhen(true)] out AST.TypedDocument? outputAST)
        {
            try
            {
                var errors = state.ErrorCount();
                outputAST = new Typechecker(metadata, state).CheckDocument(inputAST);
                return allowErrors || state.ErrorCount() == errors;
            }
            catch (Exception e)
            {
                state.AddError(e);

                outputAST = null;
                return false;
            }
        }

        private readonly Attributes.Metadata metadata;
        private readonly GenerationState state;

        private Typechecker(Attributes.Metadata metadata, GenerationState state)
        {
            this.metadata = metadata;
            this.state = state;
        }

        private T? CheckAttribute<T>(AST.UntypedAttribute ast, TokenListParser<TokenKind, T> pT) where T : class
        {
            if (ast.Value.Location.Position == ast.Value.Remainder.Position)
            {
                state.AddError(ast.Key.Span, ErrorKind.Syntax, $"attribute has no value");
                return null;
            }

            var tokens = new TokenList(ast.Value.Location.Prepend(new Token(TokenKind.EqualsSign, new TextSpan("="))).Prepend(new Token(TokenKind.Identifier, ast.Key.Span)).ToArray());
            var parse = pT(tokens);
            if (parse.HasValue)
            {
                if (parse.Remainder.Position <= ast.Value.Remainder.Position)
                {
                    return parse.Value;
                }
                else
                {
                    var badToken = parse.Remainder.First();
                    state.AddError(parse.Remainder.First().Span, ErrorKind.Syntax, $"unexpected `{badToken.ToStringValue()}`, expected `,` or `]`");
                    return null;
                }
            }
            else
            {
                state.AddError(tokens, parse);
                return null;
            }
        }

        private T? CheckAttributeValue<T>(AST.UntypedAttribute ast, TokenListParser<TokenKind, T> pT) where T : class
        {
            if (ast.Value.Location.Position == ast.Value.Remainder.Position)
            {
                state.AddError(ast.Key.Span, ErrorKind.Syntax, $"attribute has no value");
                return null;
            }

            var parse = pT(ast.Value.Location);
            if (parse.HasValue)
            {
                if (parse.Remainder.Position <= ast.Value.Remainder.Position)
                {
                    return parse.Value;
                }
                else
                {
                    var badToken = parse.Remainder.First();
                    state.AddError(parse.Remainder.First().Span, ErrorKind.Syntax, $"unexpected `{badToken.ToStringValue()}`, expected `,` or `]`");
                    return null;
                }
            }
            else
            {
                state.AddError(ast.Value.Location, parse);
                return null;
            }
        }

        private AST.DocumentAttribute? CheckDocumentAttribute(AST.UntypedAttribute ast)
        {
            if (!metadata.DocumentNames.Contains(ast.Key.Text))
            {
                var validAttributes = string.Join(", ", metadata.DocumentNames.Select(a => $"`{a}`").OrderBy(x => x));
                state.AddError(ast.Key, ErrorKind.Type, "unknown attribute {0}. expected " + validAttributes, ast.Key);
                return null;
            }

            return CheckAttribute(ast, Typed.DocumentAttribute);
        }

        private AST.ObjectAttribute? CheckObjectAttribute(AST.UntypedAttribute ast)
        {
            if (!metadata.ObjectNames.Contains(ast.Key.Text))
            {
                var validAttributes = string.Join(", ", metadata.ObjectNames.Select(a => $"`{a}`").OrderBy(x => x));
                state.AddError(ast.Key, ErrorKind.Type, "unknown attribute {0}. expected " + validAttributes, ast.Key);
                return null;
            }

            return CheckAttribute(ast, Typed.ObjectAttribute);
        }

        private AST.LineAttribute? CheckLineAttribute(AST.UntypedAttribute ast)
        {
            foreach (var attr in metadata.LineAttributes)
            {
                if (attr.Names.Contains(ast.Key.Text, StringComparer.OrdinalIgnoreCase))
                {
                    return CheckAttributeValue(ast, attr.ValueParser);
                }
            }

            var validAttributes = string.Join(", ", metadata.LineNames.Select(a => $"`{a}`").OrderBy(x => x));
            state.AddError(ast.Key, ErrorKind.Type, "unknown attribute {0}. expected " + validAttributes, ast.Key);
            return null;
        }

        private AST.EntityAttribute? CheckEntityAttribute(AST.UntypedAttribute ast)
        {
            foreach (var attr in metadata.EntityAttributes)
            {
                if (attr.Names.Contains(ast.Key.Text, StringComparer.OrdinalIgnoreCase))
                {
                    return CheckAttributeValue(ast, attr.ValueParser);
                }
            }

            var validAttributes = string.Join(", ", metadata.ObjectNames.Concat(metadata.LineNames).Distinct().Select(a => $"`{a}`").OrderBy(x => x));
            state.AddError(ast.Key, ErrorKind.Type, "unknown attribute {0}. expected " + validAttributes, ast.Key);
            return null;
        }

        private AST.TypedDocument CheckDocument(AST.UntypedDocument ast)
        {
            foreach (var invalidDeclaration in ast.Declarations.Where(d => d.Value.IsT0))
            {
                state.AddError(invalidDeclaration.Location, Typed.DocumentContent(invalidDeclaration.Location));
            }

            return new AST.TypedDocument(
                ast.Declarations.SelectMany(CheckDocumentContent).ToArray()
            );
        }

        private AST.TypedClass CheckClass(AST.UntypedClass ast)
        {
            foreach (var invalidDeclaration in ast.Declarations.Where(d => d.Value.IsT0))
            {
                state.AddError(invalidDeclaration.Location, Typed.ObjectContent(invalidDeclaration.Location));
            }

            if (ast.Declarations.Any())
            {
                return new AST.ObjectClass(
                    ast.Name,
                    ast.BaseClasses.Select(c => c.Value.Name).ToArray(),
                    ast.Attributes.Select(CheckObjectAttribute).WhereNotNull().ToArray(),
                    ast.Declarations.SelectMany(CheckObjectContent).ToArray()
                );
            }

            foreach (var attr in ast.Attributes)
            {
                if (metadata.LineOnlyNames.Contains(attr.Key.Text)) 
                {
                    return new AST.LineClass(
                        ast.Name,
                        ast.BaseClasses.Select(c => c.Value.Name).ToArray(),
                        ast.Attributes.Select(CheckLineAttribute).WhereNotNull().ToArray()
                    );
                }
                else if (metadata.ObjectOnlyNames.Contains(attr.Key.Text))
                {
                    return new AST.ObjectClass(
                        ast.Name,
                        ast.BaseClasses.Select(c => c.Value.Name).ToArray(),
                        ast.Attributes.Select(CheckObjectAttribute).WhereNotNull().ToArray(),
                        ast.Declarations.SelectMany(CheckObjectContent).ToArray()
                    );
                }
            }

            return new AST.ObjectOrLineClass(
                ast.Name,
                ast.BaseClasses.Select(c => c.Value.Name).ToArray(),
                ast.Attributes.Select(CheckEntityAttribute).WhereNotNull().ToArray()
            );
        }

        private AST.TypedObject CheckObject(AST.UntypedObject ast)
        {
            foreach (var invalidDeclaration in ast.Declarations.Where(d => d.Value.IsT0))
            {
                state.AddError(invalidDeclaration.Location, Typed.ObjectContent(invalidDeclaration.Location));
            }

            return new AST.TypedObject(
                ast.Classes.Select(c => c.Value.Name).ToArray(),
                ast.Name,
                ast.Attributes.Select(CheckObjectAttribute).WhereNotNull().ToArray(),
                ast.Declarations.SelectMany(CheckObjectContent).ToArray()
            );
        }

        private AST.TypedLine CheckLine(AST.UntypedLine ast)
        {
            return new AST.TypedLine(
                ast.Classes.Select(c => c.Value.Name).ToArray(),
                ast.Segments,
                ast.Attributes.Select(CheckLineAttribute).WhereNotNull().ToArray()
            );
        }

        private IEnumerable<AST.TypedDocumentContent> CheckDocumentContent(Macro<AST.UntypedDocumentContent> declaration) => declaration.Value.Match(
            _ => Array.Empty<AST.TypedDocumentContent>(),
            x => CheckDocumentAttribute(x) is AST.DocumentAttribute a ? new AST.TypedDocumentContent[] { a } : Array.Empty<AST.TypedDocumentContent>(),
            x => new AST.TypedDocumentContent[] { CheckClass(x) },
            x => new AST.TypedDocumentContent[] { CheckObject(x) },
            x => new AST.TypedDocumentContent[] { CheckLine(x) }
        );

        private IEnumerable<AST.TypedObjectContent> CheckObjectContent(Macro<AST.UntypedObjectContent> declaration) => declaration.Value.Match(
            _ => Array.Empty<AST.TypedObjectContent>(),
            x => CheckObjectAttribute(x) is AST.ObjectAttribute a ? new AST.TypedObjectContent[] { a } : Array.Empty<AST.TypedObjectContent>(),
            x => new AST.TypedObjectContent[] { CheckClass(x) },
            x => new AST.TypedObjectContent[] { CheckObject(x) },
            x => new AST.TypedObjectContent[] { CheckLine(x) }
        );
    }
}
