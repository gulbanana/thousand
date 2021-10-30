using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using System.Collections.Generic;
using System.Linq;
using Thousand.Parse;

namespace Thousand.LSP.Analyse
{
    // this is is like the presentation compiler to Evaluator's batch compiler
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

            foreach (var dec in doc.Syntax.Declarations)
            {
                switch (dec.Value)
                {
                    case AST.UntypedClass asClass:
                        doc.Symbols.Add(SymbolicateClass(root, dec, asClass));
                        break;

                    case AST.UntypedObject asObject:
                        doc.Symbols.Add(SymbolicateObject(root, dec, asObject));
                        break;

                    case AST.UntypedLine asLine:
                        doc.Symbols.AddRange(WalkLine(root, asLine));
                        break;

                    case AST.EmptyDeclaration asEmpty:
                        doc.ClassNames.Add(new(root, dec.Span(doc.EndSpan), true));
                        break;
                }
            }
        }

        private DocumentSymbol SymbolicateClass(UntypedScope scope, IMacro declaration, AST.UntypedClass syntax)
        {
            var result = new DocumentSymbol
            {
                Kind = SymbolKind.Class,
                Range = declaration.Span(doc.EndSpan).AsRange(),
                SelectionRange = syntax.Name.Span.AsRange(),
                Name = "class " + syntax.Name.Text,
                Children = WalkClass(scope, syntax).ToArray()
            };

            scope.Pop(syntax);

            return result;
        }

        private IEnumerable<DocumentSymbol> WalkClass(UntypedScope scope, AST.UntypedClass syntax)
        {
            analysis.ClassDefinitions[syntax] = new Location { Uri = doc.Uri, Range = syntax.Name.Span.AsRange() };

            analysis.ClassReferences.Add(new(doc.Uri, syntax, syntax.Name));

            var classes = new List<AST.UntypedClass>();
            foreach (var callMacro in syntax.BaseClasses)
            {
                doc.ClassNames.Add(new(scope, callMacro.Value == null ? callMacro.Span(doc.EndSpan) : callMacro.Value.Name.Span, false));

                if (callMacro.Value != null)
                {
                    var klass = scope.FindClass(callMacro.Value.Name);
                    analysis.ClassReferences.Add(new(doc, klass, callMacro));
                    if (klass is not null)
                    {
                        classes.Add(klass);
                    }
                }
            }
            analysis.ClassClasses[syntax] = classes;

            var allAttributes = syntax.Attributes
                .Select(a => a.Key)
                .WhereNotNull()
                .Select(k => k.Text)
                .Distinct().ToArray();

            foreach (var attribute in syntax.Attributes)
            {
                doc.Attributes.Add(new(attribute, ParentKind.Class, allAttributes, doc.EndSpan));
            }

            var contents = scope.Push("class "+syntax.Name.Text);
            foreach (var dec in syntax.Declarations)
            {
                switch (dec.Value)
                {
                    case AST.UntypedClass asClass:
                        yield return SymbolicateClass(contents, dec, asClass);
                        break;

                    case AST.UntypedObject asObject:
                        yield return SymbolicateObject(contents, dec, asObject);
                        break;

                    case AST.UntypedLine asLine:
                        foreach (var symbol in WalkLine(contents, asLine))
                        {
                            yield return symbol;
                        }
                        break;

                    case AST.EmptyDeclaration:
                        doc.ClassNames.Add(new(contents, dec.Span(doc.EndSpan), true));
                        break;
                }
            }
        }

        private DocumentSymbol SymbolicateObject(UntypedScope scope, IMacro declaration, AST.UntypedObject syntax)
        {
            var result = new DocumentSymbol
            {
                Kind = SymbolKind.Variable,
                Range = declaration.Span(doc.EndSpan).AsRange(),
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
            var first = true;
            foreach (var callMacro in syntax.Classes)
            {
                doc.ClassNames.Add(new(scope, callMacro.Value == null ? callMacro.Span(doc.EndSpan) : callMacro.Value.Name.Span, first));

                if (callMacro.Value != null)
                {
                    var klass = scope.FindClass(callMacro.Value.Name);
                    analysis.ClassReferences.Add(new(doc, klass, callMacro));
                    if (klass is not null)
                    {
                        classes.Add(klass);
                    }
                }

                first = false;
            }
            analysis.ObjectClasses[syntax] = classes;

            var allAttributes = syntax.Attributes
                .Select(a => a.Key)
                .WhereNotNull()
                .Select(k => k.Text)
                .Distinct().ToArray();

            foreach (var attribute in syntax.Attributes)
            {
                doc.Attributes.Add(new(attribute, ParentKind.Object, allAttributes, doc.EndSpan));
            }

            var contents = scope.Push("object "+ syntax.Name?.Text);
            foreach (var dec in syntax.Declarations)
            {
                switch (dec.Value)
                {
                    case AST.UntypedClass asClass:
                        yield return SymbolicateClass(contents, dec, asClass);
                        break;

                    case AST.UntypedObject asObject:
                        yield return SymbolicateObject(contents, dec, asObject);
                        break;

                    case AST.UntypedLine asLine:
                        foreach (var symbol in WalkLine(contents, asLine))
                        {
                            yield return symbol;
                        }
                        break;

                    case AST.EmptyDeclaration:
                        doc.ClassNames.Add(new(contents, dec.Span(doc.EndSpan), true));
                        break;
                }
            }
        }

        IEnumerable<DocumentSymbol> WalkLine(UntypedScope scope, AST.UntypedLine syntax)
        {
            var allAttributes = syntax.Attributes
                .Select(a => a.Key)
                .WhereNotNull()
                .Select(k => k.Text)
                .Distinct()
                .ToArray();

            if (syntax.Attributes != null)
            {
                foreach (var attribute in syntax.Attributes)
                {
                    doc.Attributes.Add(new(attribute, ParentKind.Line, allAttributes, doc.EndSpan));
                }
            }

            foreach (var segment in syntax.Segments)
            {
                if (segment.Target.IsT0)
                {
                    doc.ObjectNames.Add(new(scope, segment.Target.AsT0.Span));

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

            var first = true;
            foreach (var callMacro in syntax.Classes)
            {
                doc.ClassNames.Add(new(scope, callMacro.Value == null ? callMacro.Span(doc.EndSpan) : callMacro.Value.Name.Span, first));

                if (callMacro.Value != null)
                {
                    var klass = scope.FindClass(callMacro.Value.Name);
                    analysis.ClassReferences.Add(new(doc, klass, callMacro));
                }

                first = false;
            }
        }
    }
}
