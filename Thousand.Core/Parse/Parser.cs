using Superpower.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Token = Superpower.Model.Token<Thousand.Parse.TokenKind>;
using TokenList = Superpower.Model.TokenList<Thousand.Parse.TokenKind>;

namespace Thousand.Parse
{
    public class Parser
    {
        private readonly List<GenerationError> ws;
        private readonly List<GenerationError> es;

        public static bool TryParse(string text, List<GenerationError> warnings, List<GenerationError> errors, [NotNullWhen(true)] out AST.TypedDocument? document)
        {
            document = new Parser(warnings, errors).Parse(text);
            return !errors.Any();
        }

        private Parser(List<GenerationError> warnings, List<GenerationError> errors)
        {
            ws = warnings;
            es = errors;
        }

        private AST.TypedDocument? Parse(string text)
        {
            var tokenizer = Tokenizer.Build();

            var template = tokenizer.TryTokenize(text);
            if (!template.HasValue)
            {
                es.Add(new(template.ErrorPosition, ErrorKind.Syntax, template.FormatErrorMessageFragment()));
                return null;
            }

            var untyped = TokenParsers.UntypedDocument(template.Value);
            if (!untyped.HasValue)
            {
                es.Add(new(untyped.ErrorPosition, ErrorKind.Syntax, untyped.FormatErrorMessageFragment()));
                return null;
            }

            var instantiation = Template(template.Value, untyped.Value);
            if (es.Any())
            {
                return null;
            }

            var typed = TokenParsers.TypedDocument(instantiation);
            if (!typed.HasValue)
            {
                es.Add(new(typed.ErrorPosition, ErrorKind.Syntax, typed.FormatErrorMessageFragment()));
                return null;
            }

            return typed.Value;
        }

        private TokenList Template(TokenList template, AST.UntypedDocument untypedAST)
        {
            var splices = new List<Splice>();

            var allClasses = new HashSet<string>();
            foreach (var c in untypedAST.Declarations.Where(d => d.IsT1 || d.IsT2))
            {
                var name = c.IsT1 ? c.AsT1.Name : c.AsT2.Name;
                if (!allClasses.Add(name.Text))
                {
                    es.Add(new(name.Location.Position, ErrorKind.Reference, $"class `{name.Text}` has already been defined"));
                    continue;
                }
            }

            var templates = new Dictionary<string, AST.UntypedClass>();
            var instantiations = new Dictionary<string, List<Token[]>>();
            foreach (var c in untypedAST.Declarations.Where(d => d.IsT1).Select(d => d.AsT1))
            {
                templates.Add(c.Name.Text, c);
                instantiations.Add(c.Name.Text, new List<Token[]>());
            }

            foreach (var o in untypedAST.Declarations.Where(d => d.IsT3).Select(d => d.AsT3))
            {
                foreach (var name in o.Classes)
                {
                    if (templates.ContainsKey(name.Text))
                    {
                        var klass = templates[name.Text];
                        var instantiation = Instantiate(klass, new Dictionary<string, Token[]>
                        {
                            { "$one", new[] { new Token(TokenKind.Number, new TextSpan("1")) } },
                            { "$two", new[] { new Token(TokenKind.Number, new TextSpan("2")) } }
                        });
                        instantiations[name.Text].Add(instantiation);
                    }
                }
            }

            foreach (var o in untypedAST.Declarations.Where(d => d.IsT4).Select(d => d.AsT4))
            {
                foreach (var name in o.Classes)
                {
                    if (templates.ContainsKey(name.Text))
                    {
                        var klass = templates[name.Text];
                        var instantiation = Instantiate(klass, new Dictionary<string, Token[]>
                        {
                            { "$one", new[] { new Token(TokenKind.Number, new TextSpan("1")) } },
                            { "$two", new[] { new Token(TokenKind.Number, new TextSpan("2")) } }
                        });
                        instantiations[name.Text].Add(instantiation);
                    }
                }
            }

            // remove template declarations
            foreach (var c in templates.Values)
            {
                var replacements = new List<Token>();

                foreach (var instantiation in instantiations[c.Name.Text])
                {
                    replacements.AddRange(instantiation);
                    replacements.Add(new Token(TokenKind.LineSeparator, new TextSpan(";")));
                }

                splices.Add(new(c.Location.Position..c.Remainder.Position, replacements.ToArray()));
            }

            foreach (var splice in splices.OrderByDescending(s => s.Location.Start.Value))
            {
                template = splice.Apply(template);
            }

            return template;
        }

        private Token[] Instantiate(AST.UntypedClass klass, Dictionary<string, Token[]> substitutions)
        {
            var splices = new List<Splice>();

            // remove argument list
            splices.Add(new(klass.Arguments.Range(klass.Location.Position), Array.Empty<Token>()));

            // substitute variables into attribute list
            foreach (var a in klass.Attributes)
            {
                var replacements = new List<Token>();
                foreach (var token in a.Value.Sequence())
                {
                    if (token.Kind == TokenKind.Variable)
                    {
                        var key = token.ToStringValue();
                        if (substitutions.ContainsKey(key))
                        {
                            var value = substitutions[key];
                            replacements.AddRange(value);
                        }
                        else
                        {
                            es.Add(new(token.Position, ErrorKind.Reference, $"variable `{key}` is not defined"));
                            replacements.Add(token);
                        }
                    }
                    else
                    {
                        replacements.Add(token);
                    }
                }

                splices.Add(new(a.Value.Range(klass.Location.Position), replacements.ToArray()));
            }

            var template = new TokenList(klass.Location.Take(klass.Remainder.Position - klass.Location.Position).ToArray());
            foreach (var splice in splices.OrderByDescending(s => s.Location.Start.Value))
            {
                template = splice.Apply(template);
            }

            return template.ToArray();
        }
    }
}
