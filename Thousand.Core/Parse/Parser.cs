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

            foreach (var c in untypedAST.Declarations.Where(d => d.IsT1).Select(d => d.AsT1))
            {
                var names = new HashSet<string>();
                foreach (var v in c.Value.Arguments.Value)
                {
                    if (!names.Add(v.Text))
                    {
                        es.Add(new(c.Value.Name.Span, ErrorKind.Reference, $"variable `{v.Text}` has already been declared"));
                        return template;
                    }
                }

                templates.Add(c.Value.Name.Text, c);
                instantiations.Add(c.Value.Name.Text, new List<Token[]>());
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
            if (objekt.Invocation != null)
            {
                Invoke(objekt.Classes, objekt.Invocation);
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
            if (line.Invocation != null)
            {
                Invoke(line.Classes, line.Invocation);
            }
        }

        private void Invoke(Macro<Identifier>[] classes, Macro<Macro[]> call)
        {
            // check arguments 
            var requiredArgs = classes.Where(c => templates.ContainsKey(c.Value.Text)).SelectMany(c => templates[c.Value.Text].Value.Arguments.Value).Count();
            if (call.Value.Length != requiredArgs)
            {
                var begin = call.Location.First().Span;
                var location = begin.Source != null ?
                    new TextSpan(begin.Source, begin.Position, call.Sequence().Select(t => t.Span.Length).Sum()) :
                    new TextSpan(new TokenList(call.Sequence().ToArray()).Dump());
                es.Add(new(location, ErrorKind.Type, $"expected {requiredArgs} arguments, found {call.Value.Length}"));
                return;
            }

            // remove the invocation
            splices.Add(new(call.Range(), Array.Empty<Token>()));

            // instantiate the template
            var index = 0;
            foreach (var klassName in classes.Where(c => templates.ContainsKey(c.Value.Text)))
            {
                var template = templates[klassName.Value.Text];
                var klass = template.Value;
                var suppliedArguments = call.Value.Skip(0).Take(klass.Arguments.Value.Length).Zip(klass.Arguments.Value, Tuple.Create);
                var substitutions = suppliedArguments.ToDictionary(t => t.Item2.Span.ToStringValue(), t => t.Item1.Sequence().ToArray());

                var uniqueName = new[] {
                    new Token(
                        TokenKind.Identifier,
                        new TextSpan($"{klass.Name.Text}-{instantiations[klass.Name.Text].Count + 1}")
                    )
                };
                splices.Add(new(klassName.Range(), uniqueName));

                var instantiation = Instantiate(template, uniqueName, substitutions);
                instantiations[klass.Name.Text].Add(instantiation);

                index += klass.Arguments.Value.Length;
            }
        }

        private Token[] Instantiate(Macro<AST.UntypedClass> macro, Token[] name, Dictionary<string, Token[]> substitutions)
        {
            var klass = macro.Value;
            var relativeSplices = new List<Splice>();

            // replace name
            relativeSplices.Add(new(1..2, name));

            // remove argument list
            relativeSplices.Add(new(klass.Arguments.Range(macro.Location.Position), Array.Empty<Token>()));

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
                            es.Add(new(token.Span, ErrorKind.Reference, $"variable `{key}` is not defined"));
                            replacements.Add(token);
                        }
                    }
                    else
                    {
                        replacements.Add(token);
                    }
                }

                relativeSplices.Add(new(a.Value.Range(macro.Location.Position), replacements.ToArray()));
            }

            var template = new TokenList(macro.Location.Take(macro.Remainder.Position - macro.Location.Position).ToArray());
            foreach (var splice in relativeSplices.OrderByDescending(s => s.Location.Start.Value))
            {
                template = splice.Apply(template);
            }

            return template.ToArray();
        }
    }
}
