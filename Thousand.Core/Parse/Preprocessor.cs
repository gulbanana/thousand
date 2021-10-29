using Superpower.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Token = Superpower.Model.Token<Thousand.Parse.TokenKind>;
using TokenList = Superpower.Model.TokenList<Thousand.Parse.TokenKind>;

namespace Thousand.Parse
{
    public sealed class Preprocessor
    {
        public static bool TryPreprocess(GenerationState state, string inputText, [NotNullWhen(true)] out AST.UntypedDocument? outputSyntax)
        {
            var errors = state.ErrorCount();
            outputSyntax = Preprocess(state, inputText);
            return state.ErrorCount() == errors;
        }

        public static bool TryPreprocess(GenerationState state, TokenList inputTokens, [NotNullWhen(true)] out AST.UntypedDocument? outputSyntax)
        {
            var errors = state.ErrorCount();
            outputSyntax = Preprocess(state, inputTokens);
            return state.ErrorCount() == errors;
        }

        public static bool TryPreprocess(GenerationState state, TokenList inputTokens, AST.UntypedDocument inputSyntax, [NotNullWhen(true)] out AST.UntypedDocument? outputSyntax)
        {
            var errors = state.ErrorCount();
            outputSyntax = Preprocess(state, inputTokens, inputSyntax);
            return state.ErrorCount() == errors;
        }

        private static AST.UntypedDocument? Preprocess(GenerationState state, string text)
        {
            var tokenizer = Tokenizer.Build();

            // XXX consider merging this with the version in AnalysisService (and they could both use Tokenizer?)
            var untypedTokens = tokenizer.TryTokenize(text);
            if (!untypedTokens.HasValue)
            {
                state.AddError(untypedTokens.Location, ErrorKind.Syntax, untypedTokens.FormatErrorMessageFragment());
                return null;
            }

            return Preprocess(state, untypedTokens.Value);
        }

        private static AST.UntypedDocument? Preprocess(GenerationState state, TokenList untypedTokens)
        {
            var untypedAST = Untyped.Document(untypedTokens);
            if (!untypedAST.HasValue)
            {
                state.AddError(untypedTokens, untypedAST);
                return null;
            }

            return Preprocess(state, untypedTokens, untypedAST.Value);
        }

        private static AST.UntypedDocument? Preprocess(GenerationState state, TokenList pass1Tokens, AST.UntypedDocument pass1AST)
        {
            // resolve objects and lines with template base classes (turning those classes concrete)
            var errors = state.ErrorCount();
            var pass2Tokens = new Preprocessor(state, 1).Pass1(pass1Tokens, pass1AST);
            if (state.ErrorCount() > errors)
            {
                return null;
            }

            // repeatedly resolve concrete classes with template base classes
            var p = 2;
            var pass2AST = Untyped.Document(pass2Tokens);
            var resolveableDecs = pass2AST.Value.Declarations.Count(d => d.Value.IsT2 && Resolveable(d.Value.AsT2));
            var lastDecs = 0;
            do
            {
                if (!pass2AST.HasValue)
                {
                    state.AddError(pass2Tokens, pass2AST);
                    return null;
                }

                errors = state.ErrorCount();
                pass2Tokens = new Preprocessor(state, p++).Pass2(pass2Tokens, pass2AST.Value);
                if (state.ErrorCount() > errors)
                {
                    return null;
                }

                lastDecs = resolveableDecs;
                pass2AST = Untyped.Document(pass2Tokens);
                resolveableDecs = pass2AST.Value.Declarations.Count(d => d.Value.IsT2 && Resolveable(d.Value.AsT2));
            } while (!pass2AST.HasValue || (resolveableDecs > 0 && resolveableDecs < lastDecs));

            if (resolveableDecs > 0)
            {
                foreach (var dec in pass2AST.Value.Declarations.Where(d => d.Value.IsT2 && Resolveable(d.Value.AsT2)))
                {
                    state.AddWarning(dec.Value.AsT2.Name, ErrorKind.Internal, "Macro resolution failed.");
                }                
            }

            // remove remaining template classes
            var typedAST = new Preprocessor(state, p).Pass3(pass2AST.Value);

            return typedAST;
        }

        private static bool Resolveable(AST.UntypedClass klass)
        {
            return !klass.Arguments.Value.Any() && klass.BaseClasses.Any(b => b.Value.Arguments.Any());
        }

        private readonly GenerationState state;
        private readonly int p;
        private readonly Dictionary<(string, int), Macro<AST.UntypedClass>> templates;
        private readonly Dictionary<(string, int), List<Token[]>> instantiations;
        private readonly List<Splice> splices;

        private Preprocessor(GenerationState state, int p)
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
            if (!GatherTemplates(untypedAST))
            {
                return untypedTokens;
            }

            foreach (var o in untypedAST.Declarations.Where(d => d.Value.IsT3).Select(d => d.Value.AsT3))
            {
                ResolveObject(o);
            }

            foreach (var l in untypedAST.Declarations.Where(d => d.Value.IsT4).Select(d => d.Value.AsT4))
            {
                ResolveLine(l);
            }

            GenerateInstantiations();

            foreach (var splice in splices.OrderByDescending(s => s.Location.Start.Value))
            {
                untypedTokens = splice.Apply(untypedTokens);
            }

            return untypedTokens;
        }

        // resolve templates used by classes, which may or not have been templates themselves (repeat until none left)
        public TokenList Pass2(TokenList untypedTokens, AST.UntypedDocument untypedAST)
        {
            if (!GatherTemplates(untypedAST))
            {
                return untypedTokens;
            }

            var cl = untypedAST.Declarations.Where(d => d.Value.IsT2 && Resolveable(d.Value.AsT2)).Select(d => d.Select(x => x.AsT2)).LastOrDefault();
            if (cl != null)
            {
                ResolveClass(cl.Value);
            }

            GenerateInstantiations();

            foreach (var splice in splices.OrderByDescending(s => s.Location.Start.Value))
            {
                untypedTokens = splice.Apply(untypedTokens);
            }

            return untypedTokens;
        }

        // remove unused templates (which may have been used in previous passes)
        public AST.UntypedDocument Pass3(AST.UntypedDocument untypedAST)
        {
            return RemoveDocumentTemplates(untypedAST);
        }

        // XXX find a way to reuse this information across passes
        private bool GatherTemplates(AST.UntypedDocument untypedAST)
        {
            var allClasses = new HashSet<(string, int)>();
            foreach (var klass in untypedAST.Declarations.Where(d => d.Value.IsT2).Select(d => d.Value.AsT2))
            {
                for (var arity = klass.Arguments.Value.Length; arity >= klass.Arguments.Value.Count(a => a.Default == null); arity--)
                {
                    if (!allClasses.Add((klass.Name.Text, arity)))
                    {
                        state.AddErrorEx(klass.Name, ErrorKind.Reference, "class {0} has already been defined", (klass.Name, $"/{arity}"));
                        return false;
                    }
                }
            }

            foreach (var macro in untypedAST.Declarations.Where(d => d.Value.IsT2).Select(d => d.Select(x => x.AsT2)))
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
                        state.AddError(v.Name, ErrorKind.Reference, "parameter {0} has already been defined", v.Name);
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

                for (var arity = klass.Arguments.Value.Length; arity >= klass.Arguments.Value.Count(a => a.Default == null); arity--)
                {
                    templates.Add((klass.Name.Text, arity), macro);
                    instantiations.Add((klass.Name.Text, arity), new List<Token[]>());
                }
            }

            return true;
        }

        private void ResolveObject(AST.UntypedObject objekt)
        {
            foreach (var call in objekt.Classes)
            {
                Invoke(call);
            }

            foreach (var child in objekt.Declarations.Where(d => d.Value.IsT3).Select(d => d.Value.AsT3))
            {
                ResolveObject(child);
            }

            foreach (var child in objekt.Declarations.Where(d => d.Value.IsT4).Select(d => d.Value.AsT4))
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

            foreach (var seg in line.Segments.Where(t => t.Target.IsT1))
            {
                ResolveObject(seg.Target.AsT1.Value);
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
            if (templates.ContainsKey((call.Name.Text, call.Arguments.Length)))
            {
                var klassMacro = templates[(call.Name.Text, call.Arguments.Length)];
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

                var uniqueString = $"{klass.Name.Text}-a{call.Arguments.Length}-p{p}-{instantiations[(klass.Name.Text, call.Arguments.Length)].Count + 1}";
                var uniqueName = new[] {
                    new Token(
                        TokenKind.Identifier,
                        new TextSpan(uniqueString)
                    )
                };

                state.MapSpan(uniqueString, callMacro.Value.Name.Span);
                splices.Add(new(callMacro.Range(), uniqueName));

                var instantiation = Instantiate(klassMacro, uniqueName, substitutions);
                instantiations[(klass.Name.Text, call.Arguments.Length)].Add(instantiation);
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
            foreach (var decMacro in klass.Declarations)
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

        private void GenerateInstantiations()
        {
            foreach (var kvpg in templates.GroupBy(kvp => kvp.Value.Range()))
            {
                var replacements = new List<Token>();
                replacements.AddRange(kvpg.First().Value.Sequence());
                foreach (var kvp in kvpg)
                {
                    foreach (var instantiation in instantiations[kvp.Key])
                    {
                        replacements.Add(new Token(TokenKind.LineSeparator, new TextSpan(";")));
                        replacements.AddRange(instantiation);
                    }
                }
                splices.Add(new(kvpg.Key, replacements.ToArray()));
            }
        }

        private AST.UntypedDocument RemoveDocumentTemplates(AST.UntypedDocument ast)
        {
            return new AST.UntypedDocument(
                ast.Declarations
                    .Where(d => !d.Value.IsT2 || d.Value.AsT2.Arguments.Value.Length == 0)
                    .Select(d => d.Value.IsT2 ? d.Select(x => (AST.UntypedDocumentContent)RemoveClassTemplates(x.AsT2))
                               : d.Value.IsT3 ? d.Select(x => (AST.UntypedDocumentContent)RemoveObjectTemplates(x.AsT3))
                               : d)
                    .ToArray()
            );
        }

        private AST.UntypedClass RemoveClassTemplates(AST.UntypedClass ast)
        {
            return new AST.UntypedClass(
                ast.Name,
                ast.Arguments,
                ast.BaseClasses,
                ast.Attributes,
                ast.Declarations
                    .Where(d => !d.Value.IsT2 || d.Value.AsT2.Arguments.Value.Length == 0)
                    .Select(d => d.Value.IsT2 ? d.Select(x => (AST.UntypedObjectContent)RemoveClassTemplates(x.AsT2))
                               : d.Value.IsT3 ? d.Select(x => (AST.UntypedObjectContent)RemoveObjectTemplates(x.AsT3))
                               : d)
                    .ToArray()
            );
        }

        private AST.UntypedObject RemoveObjectTemplates(AST.UntypedObject ast)
        {
            return new AST.UntypedObject(
                ast.Classes,
                ast.Name,
                ast.Attributes,
                ast.Declarations
                    .Where(d => !d.Value.IsT2 || d.Value.AsT2.Arguments.Value.Length == 0)
                    .Select(d => d.Value.IsT2 ? d.Select(x => (AST.UntypedObjectContent)RemoveClassTemplates(x.AsT2))
                               : d.Value.IsT3 ? d.Select(x => (AST.UntypedObjectContent)RemoveObjectTemplates(x.AsT3))
                               : d)
                    .ToArray()
            );
        }
    }
}
