using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Token = Superpower.Model.Token<Thousand.Parse.TokenKind>;
using TokenList = Superpower.Model.TokenList<Thousand.Parse.TokenKind>;

namespace Thousand.Parse
{
    public class Typechecker
    {
        private readonly GenerationState state;

        public static bool TryTypecheck(GenerationState state, TokenList inputTokens, AST.UntypedDocument inputAST, bool stripErrors, [NotNullWhen(true)] out AST.TypedDocument? outputAST)
        {
            if (stripErrors)
            {
                var splices = new Typechecker(state).StripDocumentErrors(inputAST).ToList();
                foreach (var splice in splices.OrderByDescending(s => s.Location.Start.Value))
                {
                    inputTokens = splice.Apply(inputTokens);
                }
            }

            var typedAST = Typed.Document(inputTokens);
            if (!typedAST.HasValue)
            {
                var badToken = typedAST.Location.IsAtEnd ? inputTokens.Last() : typedAST.Location.First();
                state.AddError(badToken.Span, ErrorKind.Syntax, typedAST.FormatErrorMessageFragment());

                outputAST = null;
                return false;
            }
            else
            {
                outputAST = typedAST.Value;
                return true;
            }
        }

        private Typechecker(GenerationState state)
        {
            this.state = state;
        }

        private IEnumerable<Splice> StripDocumentErrors(AST.UntypedDocument ast)
        {
            foreach (var declaration in ast.Declarations)
            {
                if (declaration.Value.IsT0)
                {
                    var subTokens = new TokenList(declaration.Sequence().ToArray());
                    var error = Untyped.DocumentContent(subTokens);
                    var badToken = error.Location.IsAtEnd ? subTokens.Last() : error.Location.First();
                    state.AddError(badToken.Span, ErrorKind.Syntax, error.FormatErrorMessageFragment());

                    yield return new Splice(declaration.Range(), Array.Empty<Token>());
                }
                else if (declaration.Value.IsT2)
                {
                    foreach (var splice in StripClassErrors(declaration.Value.AsT2))
                    {
                        yield return splice;
                    }
                }
                else if (declaration.Value.IsT3)
                {
                    foreach (var splice in StripObjectErrors(declaration.Value.AsT3))
                    {
                        yield return splice;
                    }
                }
            }
        }

        private IEnumerable<Splice> StripClassErrors(AST.UntypedClass ast)
        {
            foreach (var declaration in ast.Declarations)
            {
                if (declaration.Value.IsT0)
                {
                    var subTokens = new TokenList(declaration.Sequence().ToArray());
                    var error = Untyped.DocumentContent(subTokens);
                    var badToken = error.Location.IsAtEnd ? subTokens.Last() : error.Location.First();
                    state.AddError(badToken.Span, ErrorKind.Syntax, error.FormatErrorMessageFragment());

                    yield return new Splice(declaration.Range(), Array.Empty<Token>());
                }
                else if (declaration.Value.IsT2)
                {
                    foreach (var splice in StripClassErrors(declaration.Value.AsT2))
                    {
                        yield return splice;
                    }
                }
                else if (declaration.Value.IsT3)
                {
                    foreach (var splice in StripObjectErrors(declaration.Value.AsT3))
                    {
                        yield return splice;
                    }
                }
            }
        }

        private IEnumerable<Splice> StripObjectErrors(AST.UntypedObject ast)
        {
            foreach (var declaration in ast.Declarations)
            {
                if (declaration.Value.IsT0)
                {
                    var subTokens = new TokenList(declaration.Sequence().ToArray());
                    var error = Untyped.DocumentContent(subTokens);
                    var badToken = error.Location.IsAtEnd ? subTokens.Last() : error.Location.First();
                    state.AddError(badToken.Span, ErrorKind.Syntax, error.FormatErrorMessageFragment());

                    yield return new Splice(declaration.Range(), Array.Empty<Token>());
                }
                else if (declaration.Value.IsT2)
                {
                    foreach (var splice in StripClassErrors(declaration.Value.AsT2))
                    {
                        yield return splice;
                    }
                }
                else if (declaration.Value.IsT3)
                {
                    foreach (var splice in StripObjectErrors(declaration.Value.AsT3))
                    {
                        yield return splice;
                    }
                }
            }
        }
    }
}
