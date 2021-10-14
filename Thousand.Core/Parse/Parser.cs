using Superpower.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Token = Superpower.Model.Token<Thousand.Parse.TokenKind>;
using TokenList = Superpower.Model.TokenList<Thousand.Parse.TokenKind>;

namespace Thousand.Parse
{
    public sealed class Parser
    {
        public static bool TryParse(string text, List<GenerationError> warnings, List<GenerationError> errors, [NotNullWhen(true)] out AST.TypedDocument? document)
        {
            document = Parse(text, warnings, errors);
            return !errors.Any();
        }

        private static AST.TypedDocument? Parse(string text, List<GenerationError> warnings, List<GenerationError> errors)
        {
            var tokenizer = Tokenizer.Build();

            var untypedTokens = tokenizer.TryTokenize(text);
            if (!untypedTokens.HasValue)
            {
                errors.Add(new(untypedTokens.Location, ErrorKind.Syntax, untypedTokens.FormatErrorMessageFragment()));
                return null;
            }
            var pass1Tokens = untypedTokens.Value;

            var pass1AST = Untyped.UntypedDocument(pass1Tokens);
            if (!pass1AST.HasValue)
            {
                var badToken = pass1AST.Location.IsAtEnd ? pass1Tokens.Last() : pass1AST.Location.First();
                errors.Add(new(badToken.Span, ErrorKind.Syntax, pass1AST.FormatErrorMessageFragment()));
                return null;
            }

            var pass2Tokens = new Parser(warnings, errors, 1).Pass1(pass1Tokens, pass1AST.Value);
            if (errors.Any())
            {
                return null;
            }

            var pass2AST = Untyped.UntypedDocument(pass2Tokens);
            if (!pass2AST.HasValue)
            {
                var badToken = pass2AST.Location.IsAtEnd ? pass2Tokens.Last() : pass2AST.Location.First();
                errors.Add(new(badToken.Span, ErrorKind.Syntax, pass2AST.FormatErrorMessageFragment()));
                return null;
            }

            var typedTokens = new Parser(warnings, errors, 2).Pass2(pass2Tokens, pass2AST.Value);
            if (errors.Any())
            {
                return null;
            }

            var typedAST = Typed.Document(typedTokens);
            if (!typedAST.HasValue)
            {
                var badToken = typedAST.Location.IsAtEnd ? pass1Tokens.Last() : typedAST.Location.First();
                errors.Add(new(badToken.Span, ErrorKind.Syntax, typedAST.FormatErrorMessageFragment()));
                return null;
            }

            return typedAST.Value;
        }

        private readonly List<GenerationError> ws;
        private readonly List<GenerationError> es;
        private readonly int p;
        private readonly Dictionary<string, Macro<AST.UntypedClass>> templates;
        private readonly Dictionary<string, List<Token[]>> instantiations;
        private readonly List<Splice> splices;

        private Parser(List<GenerationError> warnings, List<GenerationError> errors, int p)
        {
            ws = warnings;
            es = errors;
            this.p = p;
            templates = new();
            instantiations = new();
            splices = new();
        }

        // resolve templates used by entities, which may be still end up as subclasses of other templates
        public TokenList Pass1(TokenList untypedTokens, AST.UntypedDocument untypedAST)
        {
            if (!TypeCheck(untypedAST))
            {
                return untypedTokens;
            }

            foreach (var o in untypedAST.Declarations.Where(d => d.IsT2).Select(d => (AST.UntypedObject)d))
            {
                ResolveObject(o);
            }

            foreach (var l in untypedAST.Declarations.Where(d => d.IsT3).Select(d => (AST.UntypedLine)d))
            {
                ResolveLine(l);
            }

            foreach (var c in templates.Values)
            {
                var replacements = new List<Token>();

                replacements.AddRange(c.Sequence());
                replacements.Add(new Token(TokenKind.LineSeparator, new TextSpan(";")));

                foreach (var instantiation in instantiations[c.Value.Name.Text])
                {
                    replacements.AddRange(instantiation);
                    replacements.Add(new Token(TokenKind.LineSeparator, new TextSpan(";")));
                }

                splices.Add(new(c.Range(), replacements.ToArray()));
            }

            foreach (var splice in splices.OrderByDescending(s => s.Location.Start.Value))
            {
                untypedTokens = splice.Apply(untypedTokens);
            }

            return untypedTokens;
        }

        // resolve templates used by classes, which may or not have been templates themselves, and remove the unresolved templates
        public TokenList Pass2(TokenList untypedTokens, AST.UntypedDocument untypedAST)
        {
            if (!TypeCheck(untypedAST))
            {
                return untypedTokens;
            }

            foreach (var c in untypedAST.Declarations.Where(d => d.IsT1 && !d.AsT1.Value.Arguments.Value.Any()).Select(d => (Macro<AST.UntypedClass>)d))
            {
                ResolveClass(c.Value);
            }

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
                untypedTokens = splice.Apply(untypedTokens);
            }

            return untypedTokens;
        }

        private bool TypeCheck(AST.UntypedDocument untypedAST)
        {
            var allClasses = new HashSet<string>();
            foreach (var c in untypedAST.Declarations.Where(d => d.IsT1).Select(d => d.AsT1))
            {
                if (!allClasses.Add(c.Value.Name.Text))
                {
                    es.Add(new(c.Value.Name.Span, ErrorKind.Reference, $"class `{c.Value.Name.Text}` has already been defined"));
                    return false;
                }
            }

            foreach (var macro in untypedAST.Declarations.Where(d => d.IsT1).Select(d => d.AsT1))
            {
                var klass = macro.Value;
                if (!klass.Arguments.Value.Any())
                {
                    continue; // not a template
                }

                var names = new HashSet<string>();
                var hasDefault = false;
                foreach (var v in klass.Arguments.Value)
                {
                    if (!names.Add(v.Name.Text))
                    {
                        es.Add(new(klass.Name.Span, ErrorKind.Reference, $"variable `{v.Name.Text}` has already been declared"));
                        return false;
                    }

                    if (v.Default != null)
                    {
                        hasDefault = true;
                    }
                    else if (hasDefault)
                    {
                        es.Add(new(klass.Name.Span, ErrorKind.Type, $"class `{klass.Name.Text}` has default arguments following non-default arguments"));
                        return false;
                    }
                }

                templates.Add(klass.Name.Text, macro);
                instantiations.Add(klass.Name.Text, new List<Token[]>());
            }

            return true;
        }

        private void ResolveObject(AST.UntypedObject objekt)
        {
            foreach (var call in objekt.Classes)
            {
                Invoke(call);
            }

            foreach (var child in objekt.Children.Where(d => d.IsT1).Select(d => (AST.UntypedObject)d))
            {
                ResolveObject(child);
            }

            foreach (var child in objekt.Children.Where(d => d.IsT2).Select(d => (AST.UntypedLine)d))
            {
                ResolveLine(child);
            }
        }

        private void ResolveLine(AST.UntypedLine line)
        {
            foreach (var call in line.Classes)
            {
                Invoke(call);
            }
        }

        private void ResolveClass(AST.UntypedClass klass)
        {
            foreach (var call in klass.BaseClasses)
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
                        new TextSpan($"{call.Name.Text}-{p}-{instantiations[call.Name.Text].Count + 1}")
                    )
                };
                splices.Add(new(callMacro.Range(), uniqueName));

                var instantiation = Instantiate(klassMacro, uniqueName, substitutions);
                instantiations[call.Name.Text].Add(instantiation);
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

            // substitute variables into base class list
            foreach (var a in klass.BaseClasses.SelectMany(b => b.Value.Arguments))
            {
                var replacements = new List<Token>();
                foreach (var token in a.Sequence())
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
                }
                relativeSplices.Add(new(a.Range(macro.Location.Position), replacements.ToArray()));
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
