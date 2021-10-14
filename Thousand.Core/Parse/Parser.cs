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
        public static bool TryParse(string text, List<GenerationError> warnings, List<GenerationError> errors, [NotNullWhen(true)] out AST.TypedDocument? document)
        {
            document = new Parser(warnings, errors).Parse(text);
            return !errors.Any();
        }

        private readonly List<GenerationError> ws;
        private readonly List<GenerationError> es;
        private readonly Dictionary<string, Macro<AST.UntypedClass>> templates;
        private readonly Dictionary<string, List<Token<TokenKind>[]>> instantiations;
        private readonly List<Splice> splices;
        public AST.TypedDocument? ParsedDocument;

        private Parser(List<GenerationError> warnings, List<GenerationError> errors)
        {
            ws = warnings;
            es = errors;
            templates = new();
            instantiations = new();
            splices = new();
        }

        private AST.TypedDocument? Parse(string text)
        {
            var tokenizer = Tokenizer.Build();

            var untypedTokens = tokenizer.TryTokenize(text);
            if (!untypedTokens.HasValue)
            {
                es.Add(new(untypedTokens.Location, ErrorKind.Syntax, untypedTokens.FormatErrorMessageFragment()));
                return null;
            }

            var untypedAST = Untyped.UntypedDocument(untypedTokens.Value);
            if (!untypedAST.HasValue)
            {
                var badToken = untypedAST.Location.IsAtEnd ? untypedTokens.Value.Last() : untypedAST.Location.First();
                es.Add(new(badToken.Span, ErrorKind.Syntax, untypedAST.FormatErrorMessageFragment()));
                return null;
            }

            var typedTokens = Template(untypedTokens.Value, untypedAST.Value);
            if (es.Any())
            {
                return null;
            }

            var typedAST = Typed.Document(typedTokens);
            if (!typedAST.HasValue)
            {
                var badToken = typedAST.Location.IsAtEnd ? typedTokens.Last() : typedAST.Location.First();
                es.Add(new(badToken.Span, ErrorKind.Syntax, typedAST.FormatErrorMessageFragment()));
                return null;
            }

            return typedAST.Value;
        }

        private TokenList Template(TokenList template, AST.UntypedDocument untypedAST)
        {
            var allClasses = new HashSet<string>();
            foreach (var c in untypedAST.Declarations.Where(d => d.IsT1 || d.IsT2))
            {
                var name = c.IsT1 ? c.AsT1.Value.Name : c.AsT2.Name;
                if (!allClasses.Add(name.Text))
                {
                    es.Add(new(name.Span, ErrorKind.Reference, $"class `{name.Text}` has already been defined"));
                    return template;
                }
            }

            foreach (var macro in untypedAST.Declarations.Where(d => d.IsT1).Select(d => d.AsT1))
            {
                var klass = macro.Value;

                var names = new HashSet<string>();
                var hasDefault = false;
                foreach (var v in klass.Arguments.Value)
                {
                    if (!names.Add(v.Name.Text))
                    {
                        es.Add(new(klass.Name.Span, ErrorKind.Reference, $"variable `{v.Name.Text}` has already been declared"));
                        return template;
                    }

                    if (v.Default != null)
                    {
                        hasDefault = true;
                    }
                    else if (hasDefault)
                    {
                        es.Add(new(klass.Name.Span, ErrorKind.Type, $"class `{klass.Name.Text}` has default arguments following non-default arguments"));
                        return template;
                    }
                }

                templates.Add(klass.Name.Text, macro);
                instantiations.Add(klass.Name.Text, new List<Token[]>());
            }

            foreach (var o in untypedAST.Declarations.Where(d => d.IsT3).Select(d => (AST.UntypedObject)d))
            {
                SubstituteObject(o);
            }

            foreach (var l in untypedAST.Declarations.Where(d => d.IsT4).Select(d => (AST.UntypedLine)d))
            {
                SubstituteLine(l);
            }

            // remove template declarations
            foreach (var c in templates.Values)
            {
                var replacements = new List<Token>();

                foreach (var instantiation in instantiations[c.Value.Name.Text])
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

        private void SubstituteObject(AST.UntypedObject objekt)
        {
            foreach (var call in objekt.Classes)
            {
                Invoke(call);
            }

            foreach (var child in objekt.Children.Where(d => d.IsT1).Select(d => (AST.UntypedObject)d))
            {
                SubstituteObject(child);
            }

            foreach (var child in objekt.Children.Where(d => d.IsT2).Select(d => (AST.UntypedLine)d))
            {
                SubstituteLine(child);
            }
        }

        private void SubstituteLine(AST.UntypedLine line)
        {
            foreach (var call in line.Classes)
            {
                Invoke(call);
            }
        }

        private void Invoke(Macro<AST.ClassCall> callMacro)
        {
            var call = callMacro.Value;
            if (templates.ContainsKey(call.Name.Text))
            {
                var klassMacro = templates[call.Name.Text];
                var klass = klassMacro.Value;

                if (call.Arguments.Length < klass.Arguments.Value.Where(v => v.Default == null).Count())
                {
                    if (klass.Arguments.Value.All(v => v.Default is null))
                    {
                        es.Add(new(callMacro.Location.First().Span, ErrorKind.Type, $"expected {klass.Arguments.Value.Length} arguments, found {call.Arguments.Length}"));
                    }
                    else
                    {
                        es.Add(new(callMacro.Location.First().Span, ErrorKind.Type, $"expected {klass.Arguments.Value.Where(v => v.Default == null).Count()} to {klass.Arguments.Value.Length} arguments, found {call.Arguments.Length}"));
                    }
                    return;
                }

                var suppliedArguments = call.Arguments.Zip(klass.Arguments.Value, Tuple.Create);
                var defaultArguments = klass.Arguments.Value.Skip(call.Arguments.Length).Select(v => Tuple.Create(v.Default!, v));

                var substitutions = suppliedArguments.Concat(defaultArguments)
                    .ToDictionary(t => t.Item2.Name.Span.ToStringValue(), t => t.Item1.Sequence().ToArray());

                var uniqueName = new[] {
                    new Token(
                        TokenKind.Identifier,
                        new TextSpan($"{call.Name.Text}-{instantiations[call.Name.Text].Count + 1}")
                    )
                };
                splices.Add(new(callMacro.Range(), uniqueName));

                var instantiation = Instantiate(klassMacro, uniqueName, substitutions);
                instantiations[call.Name.Text].Add(instantiation);
            }
        }

        private Token[] Instantiate(Macro<AST.UntypedClass> klass, Token[] name, Dictionary<string, Token[]> substitutions)
        {
            var relativeSplices = new List<Splice>();

            // replace name
            relativeSplices.Add(new(1..2, name));

            // remove argument list
            relativeSplices.Add(new(klass.Value.Arguments.Range(klass.Location.Position), Array.Empty<Token>()));

            // substitute variables into attribute list
            foreach (var a in klass.Value.Attributes)
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
                            es.Add(new(token.Span, ErrorKind.Reference, $"variable `{key}` is not defined"));
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
