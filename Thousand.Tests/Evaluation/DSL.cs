using System;
using System.Linq;
using Thousand.Model;

namespace Thousand.Tests.Evaluation
{
    internal static class DSL
    {
        public static AST.LineSegment<AST.TypedObject> Segment(string target, ArrowKind? direction) => new AST.LineSegment<AST.TypedObject>(new Name(target), direction);

        public static AST.ObjectClass OClass(string name, params AST.ObjectAttribute[] attrs) => new AST.ObjectClass(new Name(name), Array.Empty<Name>(), attrs, Array.Empty<AST.TypedDeclaration>());
        public static AST.LineClass LClass(string name, params AST.LineAttribute[] attrs) => new AST.LineClass(new Name(name), Array.Empty<Name>(), attrs);

        public static AST.TypedObject Object(string klass, string name) => new AST.TypedObject(new Name[] { new(klass) }, new Name(name), Array.Empty<AST.ObjectAttribute>(), Array.Empty<AST.TypedDeclaration>());
        public static AST.TypedObject Object(string klass, string name, params AST.ObjectAttribute[] attrs) => new AST.TypedObject(new Name[] { new(klass) }, new Name(name), attrs, Array.Empty<AST.TypedDeclaration>());
        public static AST.TypedObject Object(string klass, string name, AST.ObjectAttribute[] attrs, AST.TypedDeclaration[] contents) => new AST.TypedObject(new Name[] { new(klass) }, new Name(name), attrs, contents);
        public static AST.TypedObject Object(string klass, AST.ObjectAttribute[] attrs, params AST.TypedDeclaration[] content) => new AST.TypedObject(new Name[] { new(klass) }, null, attrs, content);
        public static AST.TypedObject Object(string klass, params AST.ObjectAttribute[] attrs) => new AST.TypedObject(new Name[] { new(klass) }, null, attrs, Array.Empty<AST.TypedDeclaration>());
        public static AST.TypedObject Object(string klass, params AST.TypedDeclaration[] content) => new AST.TypedObject(new Name[] { new(klass) }, null, Array.Empty<AST.ObjectAttribute>(), content);
        public static AST.TypedObject Object(string[] classes, params AST.TypedDeclaration[] content) => new AST.TypedObject(classes.Select(c => new Name(c)).ToArray(), null, Array.Empty<AST.ObjectAttribute>(), content);

        public static AST.TypedLine Line(string klass, params AST.LineSegment<AST.TypedObject>[] segs) => new AST.TypedLine(new Name[] { new(klass) }, segs, Array.Empty<AST.LineAttribute>());

        public static AST.TypedDocument Document(params AST.TypedDeclaration[] declarations) => new AST.TypedDocument(declarations);
    }
}
