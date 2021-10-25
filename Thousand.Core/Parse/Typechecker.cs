using OneOf;
using Superpower;
using Superpower.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Thousand.Parse.Attributes;
using Token = Superpower.Model.Token<Thousand.Parse.TokenKind>;
using TokenList = Superpower.Model.TokenList<Thousand.Parse.TokenKind>;

namespace Thousand.Parse
{
    // Invariant: accepts only untyped AST which has already been preprocessed. Will fail hard on unpreprocessed input!
    public class Typechecker
    {
        public static bool TryTypecheck(Attributes.API api, GenerationState state, AST.UntypedDocument inputAST, bool allowErrors, [NotNullWhen(true)] out AST.TypedDocument? outputAST)
        {
            try
            {
                var errors = state.ErrorCount();
                outputAST = new Typechecker(api, state).CheckDocument(inputAST);
                return allowErrors || state.ErrorCount() == errors;
            }
            catch (Exception e)
            {
                state.AddError(e);

                outputAST = null;
                return false;
            }
        }

        private readonly Attributes.API api;
        private readonly GenerationState state;

        private Typechecker(Attributes.API metadata, GenerationState state)
        {
            this.api = metadata;
            this.state = state;
        }

        private T? CheckAttribute<T>(AST.UntypedAttribute ast, TokenListParser<TokenKind, T> pT) where T : class
        {
            if (!ast.HasValue)
            {
                state.AddError(ast.Value.Span(), ErrorKind.Syntax, $"attribute has no value");
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

        private T? CheckAttribute<T>(AST.UntypedAttribute ast, IEnumerable<AttributeDefinition<T>> metadata, IEnumerable<string> validNames) where T : class
        {
            if (string.IsNullOrEmpty(ast.Key.Text))
            {
                state.AddError(ast.Key, ErrorKind.Syntax, "expected attribute");
                return null;
            }

            foreach (var attr in metadata)
            {
                if (attr.Names.Contains(ast.Key.Text, StringComparer.OrdinalIgnoreCase))
                {
                    return CheckAttribute(ast, attr.ValueParser);
                }
            }

            var validAttributes = string.Join(", ", validNames.Select(a => $"`{a}`").OrderBy(x => x));
            state.AddError(ast.Key, ErrorKind.Type, "unknown attribute {0}. expected " + validAttributes, ast.Key);
            return null;
        }

        private AST.DocumentAttribute? CheckDocumentAttribute(AST.UntypedAttribute ast)
        {
            return CheckAttribute(ast, api.DocumentAttributes, api.DocumentNames);
        }

        private AST.ObjectAttribute? CheckObjectAttribute(AST.UntypedAttribute ast)
        {
            return CheckAttribute(ast, api.ObjectAttributes, api.ObjectNames);
        }

        private AST.LineAttribute? CheckLineAttribute(AST.UntypedAttribute ast)
        {
            return CheckAttribute(ast, api.LineAttributes, api.LineNames);
        }

        private AST.EntityAttribute? CheckEntityAttribute(AST.UntypedAttribute ast)
        {
            return CheckAttribute(ast, api.EntityAttributes, api.ObjectNames.Concat(api.LineNames).Distinct());
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
                if (api.LineOnlyNames.Contains(attr.Key.Text)) 
                {
                    return new AST.LineClass(
                        ast.Name,
                        ast.BaseClasses.Select(c => c.Value.Name).ToArray(),
                        ast.Attributes.Select(CheckLineAttribute).WhereNotNull().ToArray()
                    );
                }
                else if (api.ObjectOnlyNames.Contains(attr.Key.Text))
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
                ast.Segments.Select(s => new AST.LineSegment<AST.TypedObject>(s.Target.Match<OneOf<Identifier, AST.TypedObject>>(x => x, x => CheckObject(x.Value)), s.Direction)).ToArray(),
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
