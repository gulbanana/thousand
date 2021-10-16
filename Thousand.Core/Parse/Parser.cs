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
        public static bool TryParse(string text, GenerationState state, [NotNullWhen(true)] out AST.TypedDocument? document)
        {
            document = Parse(text, state);
            return !state.HasErrors();
        }

        private static AST.TypedDocument? Parse(string text, GenerationState state)
        {
            var tokenizer = Tokenizer.Build();

            var untypedTokens = tokenizer.TryTokenize(text);
            if (!untypedTokens.HasValue)
            {
                state.AddError(untypedTokens.Location, ErrorKind.Syntax, untypedTokens.FormatErrorMessageFragment());
                return null;
            }
            var pass1Tokens = untypedTokens.Value;

            // resolve objects and lines with template base classes (turning those classes concrete)
            var pass1AST = Untyped.UntypedDocument(pass1Tokens);
            if (!pass1AST.HasValue)
            {
                var badToken = pass1AST.Location.IsAtEnd ? pass1Tokens.Last() : pass1AST.Location.First();
                state.AddError(badToken.Span, ErrorKind.Syntax, pass1AST.FormatErrorMessageFragment());
                return null;
            }

            var pass2Tokens = new Parser(state, 1).Pass1(pass1Tokens, pass1AST.Value);
            if (state.HasErrors())
            {
                return null;
            }

            // repeatedly resolve concrete classes with template base classes
            var p = 2;
            var pass2AST = Untyped.UntypedDocument(pass2Tokens);
            do
            {
                if (!pass2AST.HasValue)
                {
                    var badToken = pass2AST.Location.IsAtEnd ? pass2Tokens.Last() : pass2AST.Location.First();
                    state.AddError(badToken.Span, ErrorKind.Syntax, pass2AST.FormatErrorMessageFragment());
                    return null;
                }

                pass2Tokens = new Parser(state, p++).Pass2(pass2Tokens, pass2AST.Value);
                if (state.HasErrors())
                {
                    return null;
                }

                pass2AST = Untyped.UntypedDocument(pass2Tokens);
            } while (!pass2AST.HasValue || pass2AST.Value.Declarations.Any(d => d.IsT1 && Resolveable(d.AsT1.Value)));

            // remove remaining template classes
            var pass3AST = Untyped.UntypedDocument(pass2Tokens);
            if (!pass3AST.HasValue)
            {
                var badToken = pass3AST.Location.IsAtEnd ? pass2Tokens.Last() : pass3AST.Location.First();
                state.AddError(badToken.Span, ErrorKind.Syntax, pass3AST.FormatErrorMessageFragment());
                return null;
            }

            var typedTokens = new Parser(state, p).Pass3(pass2Tokens, pass3AST.Value);
            if (state.HasErrors())
            {
                return null;
            }

            var typedAST = Typed.Document(typedTokens);
            if (!typedAST.HasValue)
            {
                var badToken = typedAST.Location.IsAtEnd ? pass1Tokens.Last() : typedAST.Location.First();
                state.AddError(badToken.Span, ErrorKind.Syntax, typedAST.FormatErrorMessageFragment());
                return null;
            }

            return typedAST.Value;
        }

        private static bool Resolveable(AST.UntypedClass klass)
        {
            return !klass.Arguments.Value.Any() && klass.BaseClasses.Any(b => b.Value.Arguments.Any());
        }

        private readonly GenerationState state;
        private readonly int p;
        private readonly Dictionary<string, Macro<AST.UntypedClass>> templates;
        private readonly Dictionary<string, List<Token[]>> instantiations;
        private readonly List<Splice> splices;

        private Parser(GenerationState state, int p)
        {
            this.state = state;
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

        // resolve templates used by classes, which may or not have been templates themselves (repeat until none left)
        public TokenList Pass2(TokenList untypedTokens, AST.UntypedDocument untypedAST)
        {
            if (!TypeCheck(untypedAST))
            {
                return untypedTokens;
            }

            var cl = untypedAST.Declarations.Where(d => d.IsT1 && Resolveable(d.AsT1.Value)).Select(d => (Macro<AST.UntypedClass>)d).LastOrDefault();
            if (cl != null)
            {
                ResolveClass(cl.Value);
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

        // remove unused templates (which may have been used in previous passes)
        public TokenList Pass3(TokenList untypedTokens, AST.UntypedDocument untypedAST)
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
                splices.Add(new(c.Range(), Array.Empty<Token>()));
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
                    state.AddError(c.Value.Name, ErrorKind.Reference, "class {0} has already been defined", c.Value.Name);
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
                        state.AddError(v.Name, ErrorKind.Reference, "parameter {0} has already been declared", v.Name);
                        return false;
                    }

                    if (v.Default != null)
                    {
                        hasDefault = true;
                    }
                    else if (hasDefault)
                    {
                        state.AddError(klass.Name, ErrorKind.Type, "class {0} has default arguments following non-default arguments", klass.Name);
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

            foreach (var child in objekt.Children.Where(d => d.IsT2).Select(d => d.AsT2))
            {
                ResolveObject(child);
            }

            foreach (var child in objekt.Children.Where(d => d.IsT3).Select(d => d.AsT3))
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
                        state.AddError(callMacro.Value.Name, ErrorKind.Type, $"expected {klass.Arguments.Value.Length} arguments, found {call.Arguments.Length}");
                    }
                    else
                    {
                        state.AddError(callMacro.Value.Name, ErrorKind.Type, $"expected {klass.Arguments.Value.Where(v => v.Default == null).Count()} to {klass.Arguments.Value.Length} arguments, found {call.Arguments.Length}");
                    }
                    return;
                }

                var suppliedArguments = call.Arguments.Zip(klass.Arguments.Value, Tuple.Create);
                var defaultArguments = klass.Arguments.Value.Skip(call.Arguments.Length).Select(v => Tuple.Create(v.Default!, v));

                var substitutions = suppliedArguments.Concat(defaultArguments)
                    .ToDictionary(t => t.Item2.Name.Span.ToStringValue(), t => t.Item1.Sequence().ToArray());

                var uniqueString = $"{call.Name.Text}-{p}-{instantiations[call.Name.Text].Count + 1}";
                var uniqueName = new[] {
                    new Token(
                        TokenKind.Identifier,
                        new TextSpan(uniqueString)
                    )
                };

                state.MapSpan(uniqueString, callMacro.Value.Name.Span);
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
                            state.AddError(token.Span, ErrorKind.Reference, $"variable `{key}` is not defined; try adding a parameter to class {{0}}", klass.Name);
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
                            state.AddError(token.Span, ErrorKind.Reference, $"variable `{key}` is not defined; try adding a parameter to class {{0}}", klass.Name);
                            replacements.Add(token);
                        }
                    }
                }
                relativeSplices.Add(new(a.Range(macro.Location.Position), replacements.ToArray()));
            }

            // substitute variables into scope
            foreach (var decMacro in klass.Children)
            {
                var replacements = new List<Token>();
                foreach (var token in decMacro.Sequence())
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
                            state.AddError(token.Span, ErrorKind.Reference, $"variable `{key}` is not defined; try adding a parameter to class {{0}}", klass.Name);
                            replacements.Add(token);
                        }
                    }
                    else
                    {
                        replacements.Add(token);
                    }
                }
                relativeSplices.Add(new(decMacro.Range(macro.Location.Position), replacements.ToArray()));
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
