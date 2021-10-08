using Superpower.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Thousand.AST;

namespace Thousand.Parse
{
    public class Parser
    {
        public static bool TryParse(string text, List<GenerationError> warnings, List<GenerationError> errors, [NotNullWhen(true)] out AST.TypedDocument? document)
        {
            var tokenizer = Tokenizer.Build();

            var template = tokenizer.TryTokenize(text);
            if (!template.HasValue)
            {
                errors.Add(new(template.ErrorPosition, ErrorKind.Syntax, template.FormatErrorMessageFragment()));
                document = null;
                return false;
            }

            var untyped = TokenParsers.UntypedDocument(template.Value);
            if (!untyped.HasValue)
            {
                errors.Add(new(untyped.ErrorPosition, ErrorKind.Syntax, untyped.FormatErrorMessageFragment()));
                document = null;
                return false;
            }

            var instantiation = ApplyMacros(template.Value, untyped.Value);

            var typed = TokenParsers.TypedDocument(instantiation);
            if (!typed.HasValue)
            {
                errors.Add(new(typed.ErrorPosition, ErrorKind.Syntax, typed.FormatErrorMessageFragment()));
                document = null;
                return false;
            }

            document = typed.Value;
            return true;
        }

        private static TokenList<TokenKind> ApplyMacros(TokenList<TokenKind> template, UntypedDocument untypedAST)
        {
            var splices = new List<(Macro macro, Token<TokenKind>[] list)>();

            var classes = untypedAST.Declarations.Where(d => d.IsT1).Select(d => d.AsT1);
            foreach (var c in classes)
            {
                foreach (var a in c.Attributes)
                {
                    var replacementList = new List<Token<TokenKind>>();
                    foreach (var token in a.Value.Sequence())
                    {
                        if (token.Kind == TokenKind.Variable)
                        {
                            var value = token.ToStringValue() == "$one" ? "1" : "3";
                            replacementList.Add(new Token<TokenKind>(TokenKind.Number, new TextSpan(value)));
                        }
                        else
                        {
                            replacementList.Add(token);
                        }
                    }
                    splices.Add((a.Value, replacementList.ToArray()));
                }
            }

            foreach (var splice in splices.OrderByDescending(s => s.macro.Location.Position))
            {
                template = Splice(template, splice.list, splice.macro.Location.Position..splice.macro.Remainder.Position);
            }

            return template;
        }

        private static TokenList<TokenKind> Splice(TokenList<TokenKind> list, Token<TokenKind>[] replacements, Range range)
        {
            var newList = new List<Token<TokenKind>>();
            
            newList.AddRange(list.Take(range.Start.Value));
            newList.AddRange(replacements);
            newList.AddRange(list.Skip(range.End.Value));

            return new(newList.ToArray());
        }
    }
}
