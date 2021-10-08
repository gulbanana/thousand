﻿using Superpower.Model;
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
        public static bool TryParse(string text, List<GenerationError> warnings, List<GenerationError> errors, [NotNullWhen(true)] out AST.TypedDocument? document)
        {
            document = new Parser(warnings, errors).Parse(text);
            return !errors.Any();
        }

        private readonly List<GenerationError> ws;
        private readonly List<GenerationError> es;
        private Dictionary<string, AST.UntypedClass> templates;
        private Dictionary<string, List<Token<TokenKind>[]>> instantiations;

        private Parser(List<GenerationError> warnings, List<GenerationError> errors)
        {
            ws = warnings;
            es = errors;
            templates = new();
            instantiations = new();
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

            foreach (var c in untypedAST.Declarations.Where(d => d.IsT1).Select(d => d.AsT1))
            {
                templates.Add(c.Name.Text, c);
                instantiations.Add(c.Name.Text, new List<Token[]>());
            }

            foreach (var o in untypedAST.Declarations.Where(d => d.IsT3).Select(d => (AST.UntypedObject)d))
            {
                foreach (var call in o.Classes)
                {
                    Invoke(splices, call);
                }
            }

            foreach (var o in untypedAST.Declarations.Where(d => d.IsT4).Select(d => (AST.UntypedLine)d))
            {
                foreach (var call in o.Classes)
                {
                    Invoke(splices, call);
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

                splices.Add(new(c.Range(), replacements.ToArray()));
            }

            foreach (var splice in splices.OrderByDescending(s => s.Location.Start.Value))
            {
                template = splice.Apply(template);
            }

            return template;
        }

        private void Invoke(List<Splice> splices, AST.ClassCall call)
        {
            if (templates.ContainsKey(call.Name.Text))
            {
                var klass = templates[call.Name.Text];

                if (klass.Arguments.Variables.Count() != call.Arguments.Count())
                {
                    es.Add(new(call.Location.First().Position, ErrorKind.Type, $"expected {klass.Arguments.Variables.Count()} arguments, found {call.Arguments.Count()}"));
                }
                var substitutions = klass.Arguments.Variables.Zip(call.Arguments, Tuple.Create).ToDictionary(t => t.Item1.Location.ToStringValue(), t => t.Item2.Sequence().ToArray());

                var uniqueName = new[] {
                    new Token(
                        TokenKind.Identifier,
                        new TextSpan($"{call.Name.Text}-{instantiations[call.Name.Text].Count + 1}")
                    )
                };
                splices.Add(new(call.Range(), uniqueName));

                var instantiation = Instantiate(klass, uniqueName, substitutions);
                instantiations[call.Name.Text].Add(instantiation);
            }
        }

        private Token[] Instantiate(AST.UntypedClass klass, Token[] name, Dictionary<string, Token[]> substitutions)
        {
            var relativeSplices = new List<Splice>();

            // replace name
            relativeSplices.Add(new(1..2, name));

            // remove argument list
            relativeSplices.Add(new(klass.Arguments.Range(klass.Location.Position), Array.Empty<Token>()));

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

                relativeSplices.Add(new(a.Value.Range(klass.Location.Position), replacements.ToArray()));
            }

            var template = new TokenList(klass.Location.Take(klass.Remainder.Position - klass.Location.Position).ToArray());
            foreach (var splice in relativeSplices.OrderByDescending(s => s.Location.Start.Value))
            {
                template = splice.Apply(template);
            }

            return template.ToArray();
        }
    }
}
