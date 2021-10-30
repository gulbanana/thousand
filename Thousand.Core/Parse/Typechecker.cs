﻿using OneOf;
using Superpower;
using Superpower.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Thousand.Parse
{
    // Invariant: accepts only untyped AST which has already been preprocessed. Will fail hard on unpreprocessed input!
    public class Typechecker
    {
        public static bool TryTypecheck(API.Metadata api, GenerationState state, AST.UntypedDocument inputAST, TextSpan inputEnd, bool allowErrors, [NotNullWhen(true)] out AST.TypedDocument? outputAST)
        {
            try
            {
                var errors = state.ErrorCount();
                outputAST = new Typechecker(api, state, inputEnd).CheckDocument(inputAST);
                return allowErrors || state.ErrorCount() == errors;
            }
            catch (Exception e)
            {
                state.AddError(e);

                outputAST = null;
                return false;
            }
        }

        private readonly API.Metadata api;
        private readonly GenerationState state;
        private readonly TextSpan endSpan;

        private Typechecker(API.Metadata api, GenerationState state, TextSpan endSpan)
        {
            this.api = api;
            this.state = state;
            this.endSpan = endSpan;
        }

        private T? CheckAttribute<T>(AST.UntypedAttribute ast, TokenListParser<TokenKind, T> pT) where T : class
        {
            if (!ast.HasValue)
            {
                if (ast.Key != null)
                {
                    state.AddError(ast.Key, ErrorKind.Syntax, $"attribute {{0}} has no value", ast.Key);
                }
                else
                {
                    state.AddError(ast.Value.Span(endSpan), ErrorKind.Syntax, $"attribute has no value");
                }
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
                state.AddError(ast.Value.Location, endSpan, parse);
                return null;
            }
        }

        private T? CheckAttribute<T>(AST.UntypedAttribute ast, IEnumerable<API.AttributeDefinition<T>> metadata, IEnumerable<string> validNames) where T : class
        {
            // XXX this will produce a bad error position
            if (string.IsNullOrEmpty(ast.Key?.Text))
            {
                state.AddError(ast.Key?.Span ?? ast.Value.Span(endSpan), ErrorKind.Syntax, "expected attribute");
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

        private AST.ObjectAttribute? CheckObjectAttribute(AST.UntypedAttribute ast)
        {
            return CheckAttribute(ast, api.ObjectDefinitions, api.ObjectNames);
        }

        private AST.LineAttribute? CheckLineAttribute(AST.UntypedAttribute ast)
        {
            return CheckAttribute(ast, api.LineDefinitions, api.LineNames);
        }

        private AST.EntityAttribute? CheckEntityAttribute(AST.UntypedAttribute ast)
        {
            return CheckAttribute(ast, api.EntityDefinitions, api.ObjectNames.Concat(api.LineNames).Distinct());
        }

        private AST.TypedDocument CheckDocument(AST.UntypedDocument ast)
        {
            foreach (var invalidDeclaration in ast.Declarations.OfType<IMacro<AST.InvalidDeclaration>>())
            {
                state.AddError(invalidDeclaration.Location, endSpan, Typed.DocumentContent(invalidDeclaration.Location));
            }

            return new AST.TypedDocument(
                ast.Declarations.SelectMany(CheckDocumentContent).ToArray()
            );
        }

        private AST.TypedClass CheckClass(AST.UntypedClass ast)
        {
            foreach (var invalidDeclaration in ast.Declarations.OfType<IMacro<AST.InvalidDeclaration>>())
            {
                state.AddError(invalidDeclaration.Location, endSpan, Typed.ObjectContent(invalidDeclaration.Location));
            }

            foreach (var missingBaseClass in ast.BaseClasses.Where(d => d.Value == null))
            {
                state.AddError(missingBaseClass.Location, endSpan, Shared.ClassReference(missingBaseClass.Location));
            }

            if (ast.Declarations.Any())
            {
                return new AST.ObjectClass(
                    ast.Name,
                    ast.BaseClasses.Select(c => c.Value).WhereNotNull().Select(c => c.Name).ToArray(),
                    ast.Attributes.Select(CheckObjectAttribute).WhereNotNull().ToArray(),
                    ast.Declarations.SelectMany(CheckObjectContent).ToArray()
                );
            }

            foreach (var attr in ast.Attributes)
            {
                if (attr.Key == null)
                {
                    state.AddError(attr.Value.Span(endSpan), ErrorKind.Syntax, "expected attribute");
                    continue;
                }
                if (api.LineOnlyNames.Contains(attr.Key.Text)) 
                {
                    return new AST.LineClass(
                        ast.Name,
                        ast.BaseClasses.Select(c => c.Value).WhereNotNull().Select(c => c.Name).ToArray(),
                        ast.Attributes.Select(CheckLineAttribute).WhereNotNull().ToArray()
                    );
                }
                else if (api.ObjectOnlyNames.Contains(attr.Key.Text))
                {
                    return new AST.ObjectClass(
                        ast.Name,
                        ast.BaseClasses.Select(c => c.Value).WhereNotNull().Select(c => c.Name).ToArray(),
                        ast.Attributes.Select(CheckObjectAttribute).WhereNotNull().ToArray(),
                        ast.Declarations.SelectMany(CheckObjectContent).ToArray()
                    );
                }
            }

            if (!ast.Attributes.IsComplete.Value)
            {
                state.AddError(ast.Attributes.IsComplete.Span(endSpan), ErrorKind.Syntax, "expected `]`");
            }

            return new AST.ObjectOrLineClass(
                ast.Name,
                ast.BaseClasses.Select(c => c.Value).WhereNotNull().Select(c => c.Name).ToArray(),
                ast.Attributes.Select(CheckEntityAttribute).WhereNotNull().ToArray()
            );
        }

        private AST.TypedObject CheckObject(AST.UntypedObject ast)
        {
            foreach (var invalidDeclaration in ast.Declarations.OfType<IMacro<AST.InvalidDeclaration>>())
            {
                state.AddError(invalidDeclaration.Location, endSpan, Typed.ObjectContent(invalidDeclaration.Location));
            }

            foreach (var missingClass in ast.Classes.Where(d => d.Value == null))
            {
                state.AddError(missingClass.Location, endSpan, Shared.ClassReference(missingClass.Location));
            }

            if (!ast.Attributes.IsComplete.Value)
            {
                state.AddError(ast.Attributes.IsComplete.Span(endSpan), ErrorKind.Syntax, "expected `]`");
            }

            return new AST.TypedObject(
                ast.Classes.Select(c => c.Value).WhereNotNull().Select(c => c.Name).ToArray(),
                ast.Name,
                ast.Attributes.Select(CheckObjectAttribute).WhereNotNull().ToArray(),
                ast.Declarations.SelectMany(CheckObjectContent).ToArray()
            );
        }

        private AST.TypedLine CheckLine(AST.UntypedLine ast)
        {
            foreach (var missingClass in ast.Classes.Where(d => d.Value == null))
            {
                state.AddError(missingClass.Location, endSpan, Shared.ClassReference(missingClass.Location));
            }

            if (!ast.Attributes.IsComplete.Value)
            {
                state.AddError(ast.Attributes.IsComplete.Span(endSpan), ErrorKind.Syntax, "expected `]`");
            }

            return new AST.TypedLine(
                ast.Classes.Select(c => c.Value).WhereNotNull().Select(c => c.Name).ToArray(),
                ast.Segments.Select(s => new AST.LineSegment<AST.TypedObject>(s.Target.Match<OneOf<Identifier, AST.TypedObject>>(x => x, x => CheckObject(x.Value)), s.Direction)).ToArray(),
                ast.Attributes.Select(CheckLineAttribute).WhereNotNull().ToArray()
            );
        }

        private IEnumerable<AST.TypedDocumentContent> CheckDocumentContent(IMacro<AST.UntypedDeclaration> declaration) => declaration.Value switch
        {
            AST.UntypedClass c => new AST.TypedDocumentContent[] { CheckClass(c) },
            AST.UntypedObject o => new AST.TypedDocumentContent[] { CheckObject(o) },
            AST.UntypedLine l => new AST.TypedDocumentContent[] { CheckLine(l) },
            _ => Array.Empty<AST.TypedDocumentContent>()
        };

        private IEnumerable<AST.TypedObjectContent> CheckObjectContent(IMacro<AST.UntypedDeclaration> declaration) => declaration.Value switch
        { 
            AST.UntypedClass c => new AST.TypedObjectContent[] { CheckClass(c) },
            AST.UntypedObject o => new AST.TypedObjectContent[] { CheckObject(o) },
            AST.UntypedLine l => new AST.TypedObjectContent[] { CheckLine(l) },
            _ => Array.Empty<AST.TypedObjectContent>()
        };
    }
}
