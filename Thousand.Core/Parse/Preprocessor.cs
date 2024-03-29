﻿using Superpower.Model;
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

        public static bool TryPreprocess(GenerationState state, TextSpan endSpan, TokenList inputTokens, [NotNullWhen(true)] out AST.UntypedDocument? outputSyntax)
        {
            var errors = state.ErrorCount();
            outputSyntax = Preprocess(state, endSpan, inputTokens);
            return state.ErrorCount() == errors;
        }

        public static bool TryPreprocess(GenerationState state, TextSpan endSpan, TokenList inputTokens, AST.UntypedDocument inputSyntax, [NotNullWhen(true)] out AST.UntypedDocument? outputSyntax)
        {
            var errors = state.ErrorCount();
            outputSyntax = Preprocess(state, endSpan, inputTokens, inputSyntax);
            return state.ErrorCount() == errors;
        }

        private static AST.UntypedDocument? Preprocess(GenerationState state, string source)
        {
            var tokenizer = new Tokenizer();

            // XXX consider merging this with the version in AnalysisService (and they could both use Tokenizer?)
            var untypedTokens = tokenizer.TryTokenize(source);
            if (!untypedTokens.HasValue)
            {
                state.AddError(untypedTokens.Location, ErrorKind.Syntax, untypedTokens.FormatErrorMessageFragment());
                return null;
            }

            return Preprocess(state, Shared.GetEnd(source), untypedTokens.Value);
        }

        private static AST.UntypedDocument? Preprocess(GenerationState state, TextSpan endSpan, TokenList untypedTokens)
        {
            var untypedAST = Untyped.Document(untypedTokens);
            if (!untypedAST.HasValue)
            {
                state.AddError(untypedTokens, endSpan, untypedAST);
                return null;
            }

            return Preprocess(state, endSpan, untypedTokens, untypedAST.Value);
        }

        private static AST.UntypedDocument? Preprocess(GenerationState state, TextSpan endSpan, TokenList pass1Tokens, AST.UntypedDocument pass1AST)
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
            do
            {
                if (!pass2AST.HasValue)
                {
                    state.AddError(pass2Tokens, endSpan, pass2AST);
                    return null;
                }

                errors = state.ErrorCount();
                pass2Tokens = new Preprocessor(state, p++).Pass2(pass2Tokens, pass2AST.Value);
                if (state.ErrorCount() > errors)
                {
                    return null;
                }

                pass2AST = Untyped.Document(pass2Tokens);
            } while (!pass2AST.HasValue || pass2AST.Value.Declarations.OfType<IMacro<AST.UntypedClass>>().Any(d => Resolveable(d.Value)));

            // remove remaining template classes
            var typedAST = new Preprocessor(state, p).Pass3(pass2AST.Value);

            return typedAST;
        }

        private static bool Resolveable(AST.UntypedClass klass)
        {
            return !klass.Arguments.Value.Any() && klass.BaseClasses.Any(b => b.Value != null && b.Value.Arguments.Any());
        }

        private readonly GenerationState state;
        private readonly int p;
        private readonly Dictionary<(string, int), IMacro<AST.UntypedClass>> templates;
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

            foreach (var o in untypedAST.Declarations.OfType<IMacro<AST.UntypedObject>>())
            {
                ResolveObject(o.Value);
            }

            foreach (var l in untypedAST.Declarations.OfType<IMacro<AST.UntypedLine>>())
            {
                ResolveLine(l.Value);
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

            var cl = untypedAST.Declarations.OfType<IMacro<AST.UntypedClass>>().Where(d => Resolveable(d.Value)).LastOrDefault();
            if (cl != null)
            {
                ResolveClass(cl.Value);
                if (!instantiations.Any(kvp => kvp.Value.Any()))
                {
                    state.AddError(cl.Value.Name, ErrorKind.Internal, "failed to resolve macro in base classes of {0}.", cl.Value.Name);
                    return untypedTokens;
                }
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
            foreach (var klass in untypedAST.Declarations.OfType<IMacro<AST.UntypedClass>>().Select(d => d.Value))
            {
                for (var arity = klass.Arguments.Value.Length; arity >= klass.Arguments.Value.Count(a => a.Default == null); arity--)
                {
                    if (!allClasses.Add((klass.Name.AsKey, arity)))
                    {
                        state.AddErrorEx(klass.Name, ErrorKind.Reference, "class {0} has already been defined", (klass.Name, $"/{arity}"));
                        return false;
                    }
                }
            }

            foreach (var macro in untypedAST.Declarations.OfType<IMacro<AST.UntypedClass>>())
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
                    if (!names.Add(v.Name.AsKey))
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
                    templates.Add((klass.Name.AsKey, arity), macro);
                    instantiations.Add((klass.Name.AsKey, arity), new List<Token[]>());
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

            foreach (var child in objekt.Declarations.OfType<IMacro<AST.UntypedObject>>())
            {
                ResolveObject(child.Value);
            }

            foreach (var child in objekt.Declarations.OfType<IMacro<AST.UntypedLine>>())
            {
                ResolveLine(child.Value);
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
                ResolveObject(seg.Target.AsT1.Declaration.Value);
            }
        }

        private void ResolveClass(AST.UntypedClass klass)
        {
            foreach (var call in klass.BaseClasses)
            {
                Invoke(call);
            }
        }

        private void Invoke(IMacro<AST.ClassCall?> callMacro)
        {
            if (callMacro.Value == null)
            {
                return;
            }

            var call = callMacro.Value;
            if (templates.ContainsKey((call.Name.AsKey, call.Arguments.Length)))
            {
                var klassMacro = templates[(call.Name.AsKey, call.Arguments.Length)];
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
                    .ToDictionary(t => t.Item2.Name.AsLoc.ToStringValue(), t => t.Item1.Sequence().ToArray());

                var uniqueString = $"{klass.Name.AsKey}-a{call.Arguments.Length}-p{p}-{instantiations[(klass.Name.AsKey, call.Arguments.Length)].Count + 1}";
                var uniqueName = new[] {
                    new Token(
                        TokenKind.Identifier,
                        new TextSpan(uniqueString)
                    )
                };

                state.MapSpan(uniqueString, callMacro.Value.Name.AsLoc);
                splices.Add(new(callMacro.Range(), uniqueName));

                var instantiation = Instantiate(klassMacro, uniqueName, substitutions);
                instantiations[(klass.Name.AsKey, call.Arguments.Length)].Add(instantiation);
            }
            else if (call.Arguments.Any())
            {
                state.AddErrorEx(call.Name, ErrorKind.Reference, "class {0} is not defined (classes with arguments can only be defined in the root scope)", (call.Name, $"/{call.Arguments.Length}"));
            }
        }

        private Token[] Instantiate(IMacro<AST.UntypedClass> macro, Token[] name, Dictionary<string, Token[]> substitutions)
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
                relativeSplices.Add(new(a.Value.Range(macro.Location.Position), replacements));
            }

            // substitute variables into base class list
            foreach (var a in klass.BaseClasses.Select(b => b.Value).WhereNotNull().SelectMany(b => b.Arguments))
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
                relativeSplices.Add(new(a.Range(macro.Location.Position), replacements));
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
                relativeSplices.Add(new(decMacro.Range(macro.Location.Position), replacements));
            }

            var template = macro.Location.Take(macro.Remainder.Position - macro.Location.Position).ToArray();
            foreach (var splice in relativeSplices.OrderByDescending(s => s.Location.Start.Value))
            {
                template = splice.Apply(template);
            }

            return template;
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
                splices.Add(new(kvpg.Key, replacements));
            }
        }

        private static AST.UntypedDocument RemoveDocumentTemplates(AST.UntypedDocument ast)
        {
            return new AST.UntypedDocument(
                ast.Declarations
                    .Where(d => d.Value is not AST.UntypedClass c || c.Arguments.Value.Length == 0)
                    .Select(d => d switch
                    {
                        IMacro<AST.UntypedClass> c => c.Select(RemoveClassTemplates),
                        IMacro<AST.UntypedObject> o => o.Select(RemoveObjectTemplates),
                        _ => d
                    })
                    //.Select(d => d.Select(v => v switch 
                    //{ 
                    //    AST.UntypedClass c => RemoveClassTemplates(c), 
                    //    AST.UntypedObject o => RemoveObjectTemplates(o), 
                    //    _ => v 
                    //}))
                    .ToArray()
            );
        }

        private static AST.UntypedClass RemoveClassTemplates(AST.UntypedClass ast)
        {
            return new AST.UntypedClass(
                ast.Name,
                ast.Arguments,
                ast.BaseClasses,
                ast.Attributes,
                ast.Declarations
                    .Where(d => d.Value is not AST.UntypedClass c || c.Arguments.Value.Length == 0)
                    .Select(d => d.Select(v => v switch
                    {
                        AST.UntypedClass c => RemoveClassTemplates(c),
                        AST.UntypedObject o => RemoveObjectTemplates(o),
                        _ => v
                    }))
                    .ToArray()
            );
        }

        private static AST.UntypedObject RemoveObjectTemplates(AST.UntypedObject ast)
        {
            return new AST.UntypedObject(
                ast.Classes,
                ast.Name,
                ast.Attributes,
                ast.Declarations
                    .Where(d => d.Value is not AST.UntypedClass c || c.Arguments.Value.Length == 0)
                    .Select(d => d.Select(v => v switch
                    {
                        AST.UntypedClass c => RemoveClassTemplates(c),
                        AST.UntypedObject o => RemoveObjectTemplates(o),
                        _ => v
                    }))
                    .ToArray()
            );
        }
    }
}
