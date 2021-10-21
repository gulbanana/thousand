using Superpower.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using Token = Superpower.Model.Token<Thousand.Parse.TokenKind>;

namespace Thousand.Parse
{
    // Invariant: accepts only untyped AST which has already been preprocessed. Will fail hard on unpreprocessed input!
    public class Typechecker
    {
        public static bool TryTypecheck(GenerationState state, AST.UntypedDocument inputAST, bool allowErrors, out AST.TypedDocument outputAST)
        {
            var errors = state.ErrorCount();
            outputAST = new Typechecker(state).TypecheckDocument(inputAST);
            return allowErrors || state.ErrorCount() == errors;
        }

        private readonly GenerationState state;
        private readonly HashSet<string> linesOnly;
        private readonly HashSet<string> objectsOnly;

        private Typechecker(GenerationState state)
        {
            this.state = state;

            linesOnly = Enum.GetNames<Attributes.ArrowAttributeKind>()
                .Select(Identifier.UnCamel)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            objectsOnly = Enum.GetNames<Attributes.NodeAttributeKind>()
                .Concat(Enum.GetNames<Attributes.RegionAttributeKind>())
                .Concat(Enum.GetNames<Attributes.TextAttributeKind>())
                .Select(Identifier.UnCamel)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        private AST.TypedDocument TypecheckDocument(AST.UntypedDocument ast)
        {
            foreach (var invalidDeclaration in ast.Declarations.Where(d => d.Value.IsT0))
            {
                RecordError(Typed.DocumentContent(invalidDeclaration.Location));
            }

            return new AST.TypedDocument(
                ast.Declarations.Where(d => !d.Value.IsT0).Select(d => d.Value.Match<AST.TypedDocumentContent>(
                    _ => throw new NotSupportedException(), 
                    x => TypecheckDocumentAttribute(x), 
                    x => TypecheckClass(x), 
                    x => TypecheckObject(x), 
                    x => TypecheckLine(x)
                )).ToArray()
            );
        }

        private AST.DiagramAttribute TypecheckDocumentAttribute(AST.UntypedAttribute ast)
        {
            var tokens = ast.Value.Sequence().Prepend(new Token(TokenKind.EqualsSign, new TextSpan("="))).Prepend(new Token(TokenKind.Identifier, ast.Key.Span)).ToArray();
            return Typed.DiagramAttribute(new(tokens)).Value;
        }

        private AST.ObjectAttribute TypecheckObjectAttribute(AST.UntypedAttribute ast)
        {
            var tokens = ast.Value.Sequence().Prepend(new Token(TokenKind.EqualsSign, new TextSpan("="))).Prepend(new Token(TokenKind.Identifier, ast.Key.Span)).ToArray();
            return Typed.ObjectAttribute(new(tokens)).Value;
        }

        private AST.SegmentAttribute TypecheckLineAttribute(AST.UntypedAttribute ast)
        {
            var tokens = ast.Value.Sequence().Prepend(new Token(TokenKind.EqualsSign, new TextSpan("="))).Prepend(new Token(TokenKind.Identifier, ast.Key.Span)).ToArray();
            return Typed.SegmentAttribute(new(tokens)).Value;
        }

        private AST.EntityAttribute TypecheckEntityAttribute(AST.UntypedAttribute ast)
        {
            var tokens = ast.Value.Sequence().Prepend(new Token(TokenKind.EqualsSign, new TextSpan("="))).Prepend(new Token(TokenKind.Identifier, ast.Key.Span)).ToArray();
            return Typed.EntityAttribute(new(tokens)).Value;
        }

        private AST.TypedClass TypecheckClass(AST.UntypedClass ast)
        {
            foreach (var invalidDeclaration in ast.Declarations.Where(d => d.Value.IsT0))
            {
                RecordError(Typed.ObjectContent(invalidDeclaration.Location));
            }

            if (ast.Declarations.Any())
            {
                return new AST.ObjectClass(
                    ast.Name,
                    ast.BaseClasses.Select(c => c.Value.Name).ToArray(),
                    ast.Attributes.Select(TypecheckObjectAttribute).ToArray(),
                    ast.Declarations.Where(d => !d.Value.IsT0).Select(d => d.Value.Match<AST.TypedObjectContent>(
                        _ => throw new NotSupportedException(),
                        x => TypecheckObjectAttribute(x),
                        x => TypecheckClass(x),
                        x => TypecheckObject(x),
                        x => TypecheckLine(x)
                    )).ToArray()
                );
            }

            foreach (var attr in ast.Attributes)
            {
                if (linesOnly.Contains(attr.Key.Text)) 
                {
                    return new AST.LineClass(
                        ast.Name,
                        ast.BaseClasses.Select(c => c.Value.Name).ToArray(),
                        ast.Attributes.Select(TypecheckLineAttribute).ToArray()
                    );
                }
                else if (objectsOnly.Contains(attr.Key.Text))
                {
                    return new AST.ObjectClass(
                        ast.Name,
                        ast.BaseClasses.Select(c => c.Value.Name).ToArray(),
                        ast.Attributes.Select(TypecheckObjectAttribute).ToArray(),
                        ast.Declarations.Where(d => !d.Value.IsT0).Select(d => d.Value.Match<AST.TypedObjectContent>(
                            _ => throw new NotSupportedException(),
                            x => TypecheckObjectAttribute(x),
                            x => TypecheckClass(x),
                            x => TypecheckObject(x),
                            x => TypecheckLine(x)
                        )).ToArray()
                    );
                }
            }

            return new AST.ObjectOrLineClass(
                ast.Name,
                ast.BaseClasses.Select(c => c.Value.Name).ToArray(),
                ast.Attributes.Select(TypecheckEntityAttribute).ToArray()
            );
        }

        private AST.TypedObject TypecheckObject(AST.UntypedObject ast)
        {
            foreach (var invalidDeclaration in ast.Declarations.Where(d => d.Value.IsT0))
            {
                RecordError(Typed.ObjectContent(invalidDeclaration.Location));
            }

            return new AST.TypedObject(
                ast.Classes.Select(c => c.Value.Name).ToArray(),
                ast.Name,
                ast.Attributes.Select(TypecheckObjectAttribute).ToArray(),
                ast.Declarations.Where(d => !d.Value.IsT0).Select(d => d.Value.Match<AST.TypedObjectContent>(
                    _ => throw new NotSupportedException(),
                    x => TypecheckObjectAttribute(x),
                    x => TypecheckClass(x),
                    x => TypecheckObject(x),
                    x => TypecheckLine(x)
                )).ToArray()
            );
        }

        private AST.TypedLine TypecheckLine(AST.UntypedLine ast)
        {
            return new AST.TypedLine(
                ast.Classes.Select(c => c.Value.Name).ToArray(),
                ast.Segments,
                ast.Attributes.Select(TypecheckLineAttribute).ToArray()
            );
        }

        private void RecordError<T>(TokenListParserResult<TokenKind, T> error)
        {
            var badSpan = error.Location.IsAtEnd ? TextSpan.Empty : error.Location.First().Span;
            state.AddError(badSpan, ErrorKind.Syntax, error.FormatErrorMessageFragment());
        }
    }
}
