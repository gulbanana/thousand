using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using System.Collections.Generic;
using System.Linq;
using Thousand.Parse;

namespace Thousand.LSP.Analyse
{
    // XXX this is basically just Evaluator through a kaleidoscope. can/should they be merged?
    class Walker
    {
        public static void WalkDocument(Analysis analysis, ParsedDocument doc, UntypedScope root)
        {
            new Walker(analysis, doc, root);
        }

        private readonly Analysis analysis;
        private readonly ParsedDocument doc;

        private Walker(Analysis analysis, ParsedDocument doc, UntypedScope root)
        {
            this.analysis = analysis;
            this.doc = doc;

            var allAttributes = doc.Syntax.Declarations.Where(d => d.Value.IsT1).Select(d => d.Value.AsT1.Key.Text).Distinct().ToArray();

            foreach (var dec in doc.Syntax.Declarations)
            {
                dec.Value.Switch(invalid => { }, asAttribute =>
                {
                    doc.Attributes.Add(new(asAttribute, ParentKind.Document, allAttributes));
                }, asClass =>
                {
                    doc.Symbols.Add(SymbolicateClass(root, dec, asClass));
                }, asObject =>
                {
                    doc.Symbols.Add(SymbolicateObject(root, dec, asObject));
                }, asLine =>
                {
                    doc.Symbols.AddRange(WalkLine(root, asLine));
                }, asEmpty =>
                {
                    doc.ClassNames.Add(new(root, dec.Span()));
                });
            }
        }

        private DocumentSymbol SymbolicateClass(UntypedScope scope, Macro declaration, AST.UntypedClass syntax)
        {
            var result = new DocumentSymbol
            {
                Kind = SymbolKind.Class,
                Range = declaration.Span().AsRange(),
                SelectionRange = syntax.Name.Span.AsRange(),
                Name = "class " + syntax.Name.Text,
                Children = WalkClass(scope, syntax).ToArray()
            };

            scope.Pop(syntax);

            return result;
        }

        private IEnumerable<DocumentSymbol> WalkClass(UntypedScope scope, AST.UntypedClass ast)
        {
            analysis.ClassDefinitions[ast] = new Location { Uri = doc.Uri, Range = ast.Name.Span.AsRange() };

            analysis.ClassReferences.Add(new(doc.Uri, ast, ast.Name));

            var classes = new List<AST.UntypedClass>();
            foreach (var callMacro in ast.BaseClasses)
            {
                doc.ClassNames.Add(new(scope, callMacro.Value == null ? callMacro.Span() : callMacro.Value.Name.Span));

                if (callMacro.Value != null)
                {
                    var klass = scope.FindClass(callMacro.Value.Name);
                    analysis.ClassReferences.Add(new(doc.Uri, klass, callMacro));
                    if (klass is not null)
                    {
                        classes.Add(klass);
                    }
                }
            }
            analysis.ClassClasses[ast] = classes;

            var allAttributes = ast.Attributes.Concat(ast.Declarations.Where(d => d.Value.IsT1).Select(d => d.Value.AsT1)).Select(a => a.Key.Text).Distinct().ToArray();
            foreach (var attribute in ast.Attributes)
            {
                doc.Attributes.Add(new(attribute, ParentKind.Class, allAttributes));
            }

            var contents = scope.Push("class "+ast.Name.Text);
            foreach (var dec in ast.Declarations)
            {
                if (dec.Value.IsT1)
                {
                    doc.Attributes.Add(new(dec.Value.AsT1, ParentKind.Class, allAttributes));
                }
                else if (dec.Value.IsT2)
                {
                    yield return SymbolicateClass(contents, dec, dec.Value.AsT2);
                }
                else if (dec.Value.IsT3)
                {
                    yield return SymbolicateObject(contents, dec, dec.Value.AsT3);
                }
                else if (dec.Value.IsT4)
                {
                    foreach (var symbol in WalkLine(contents, dec.Value.AsT4))
                    {
                        yield return symbol;
                    }
                } 
                else if (dec.Value.IsT5)
                {
                    doc.ClassNames.Add(new(contents, dec.Span()));
                }
            }
        }

        private DocumentSymbol SymbolicateObject(UntypedScope scope, Macro declaration, AST.UntypedObject syntax)
        {
            var result = new DocumentSymbol
            {
                Kind = SymbolKind.Object,
                Range = declaration.Span().AsRange(),
                SelectionRange = (syntax.Name?.Span ?? syntax.TypeSpan).AsRange(),
                Name = syntax.TypeName + (syntax.Name == null ? "" : $" {syntax.Name.Text}"),
                Children = WalkObject(scope, syntax).ToArray()
            };

            scope.Pop(syntax);

            return result;
        }

        private IEnumerable<DocumentSymbol> WalkObject(UntypedScope scope, AST.UntypedObject syntax)
        {
            analysis.ObjectDefinitions[syntax] = new Location { Uri = doc.Uri, Range = (syntax.Name?.Span ?? syntax.TypeSpan).AsRange() };

            if (syntax.Name != null)
            {
                analysis.ObjectReferences.Add(new(doc.Uri, syntax, syntax.Name));
            }

            var classes = new List<AST.UntypedClass>();
            foreach (var callMacro in syntax.Classes)
            {
                doc.ClassNames.Add(new(scope, callMacro.Value == null ? callMacro.Span() : callMacro.Value.Name.Span));

                if (callMacro.Value != null)
                {
                    var klass = scope.FindClass(callMacro.Value.Name);
                    analysis.ClassReferences.Add(new(doc.Uri, klass, callMacro));
                    if (klass is not null)
                    {
                        classes.Add(klass);
                    }
                }
            }
            analysis.ObjectClasses[syntax] = classes;

            var allAttributes = syntax.Attributes.Concat(syntax.Declarations.Where(d => d.Value.IsT1).Select(d => d.Value.AsT1)).Select(a => a.Key.Text).Distinct().ToArray();
            foreach (var attribute in syntax.Attributes)
            {
                doc.Attributes.Add(new(attribute, ParentKind.Object, allAttributes));
            }

            var contents = scope.Push("object "+ syntax.Name?.Text);
            foreach (var dec in syntax.Declarations)
            {
                if (dec.Value.IsT1)
                {
                    doc.Attributes.Add(new(dec.Value.AsT1, ParentKind.Object, allAttributes));
                }
                else if (dec.Value.IsT2)
                {
                    yield return SymbolicateClass(contents, dec, dec.Value.AsT2);
                }
                else if (dec.Value.IsT3)
                {
                    yield return SymbolicateObject(contents, dec, dec.Value.AsT3);
                }
                else if (dec.Value.IsT4)
                {
                    foreach (var symbol in WalkLine(contents, dec.Value.AsT4))
                    {
                        yield return symbol;
                    }
                }
                else if (dec.Value.IsT5)
                {
                    doc.ClassNames.Add(new(contents, dec.Span()));
                }
            }
        }

        IEnumerable<DocumentSymbol> WalkLine(UntypedScope scope, AST.UntypedLine syntax)
        {
            var allAttributes = syntax.Attributes.Select(a => a.Key.Text).Distinct().ToArray();
            if (syntax.Attributes != null)
            {
                foreach (var attribute in syntax.Attributes)
                {
                    doc.Attributes.Add(new(attribute, ParentKind.Line, allAttributes));
                }
            }

            foreach (var segment in syntax.Segments)
            {
                if (segment.Target.IsT0)
                {
                    if (scope.FindObject(segment.Target.AsT0) is AST.UntypedObject objekt)
                    {
                        analysis.ObjectReferences.Add(new(doc.Uri, objekt, segment.Target.AsT0));
                    }
                }
                else if (segment.Target.IsT1)
                {
                    yield return SymbolicateObject(scope, segment.Target.AsT1, segment.Target.AsT1.Value);
                }
            }

            foreach (var callMacro in syntax.Classes)
            {
                doc.ClassNames.Add(new(scope, callMacro.Value == null ? callMacro.Span() : callMacro.Value.Name.Span));

                if (callMacro.Value != null)
                {
                    var klass = scope.FindClass(callMacro.Value.Name);
                    analysis.ClassReferences.Add(new(doc.Uri, klass, callMacro));
                }
            }
        }
    }
}
