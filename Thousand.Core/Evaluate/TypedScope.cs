using System;
using System.Collections.Generic;
using System.Text;

namespace Thousand.Evaluate
{
    internal sealed class TypedScope
    {
        private readonly string name;
        private readonly GenerationState state;
        public TypedScope? Parent { get; private init; }

        private readonly Dictionary<string, IR.Node> canonicalObjects = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, IR.Node> bubbledObjects = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, ObjectContent> objectClasses = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, LineContent> lineClasses = new(StringComparer.OrdinalIgnoreCase);

        public TypedScope(string name, GenerationState state)
        {
            this.name = name;
            this.state = state;
        }

        public TypedScope Chain(string name)
        {
            return new TypedScope(name, state) { Parent = this };
        }

        public bool HasRequiredClass(Parse.Identifier b)
        {
            if (FindObjectClass(b, false).Found || FindLineClass(b, false).Found)
            {
                return true;
            }
            else
            {
                state.AddError(b, ErrorKind.Reference, $"class {{0}} is not defined in scope `{RecursiveName()}`", b);
                return false;
            }
        }

        public IEnumerable<IR.Node> GetObjects()
        {
            return canonicalObjects.Values;
        }

        public ObjectContent FindObjectClass(Parse.Identifier name, bool warn)
        {
            var result = new ObjectContent(false, Array.Empty<AST.ObjectAttribute>(), Array.Empty<AST.TypedDeclaration>());

            if (objectClasses.TryGetValue(name.Text, out var newResult))
            {
                result = newResult;
            }
            else if (Parent != null)
            {
                result = Parent.FindObjectClass(name, false);
            }

            if (!result.Found && warn)
            {
                if (FindLineClass(name, false).Found)
                {
                    state.AddWarning(name, ErrorKind.Type, "class {0} can only be used for lines, not objects", name);
                }
                else
                {
                    state.AddWarning(name, ErrorKind.Reference, $"class {{0}} is not defined in scope `{RecursiveName()}`", name);
                }
            }

            return result;
        }

        public LineContent FindLineClass(Parse.Identifier name, bool warn)
        {
            var result = new LineContent(false, Array.Empty<AST.LineAttribute>());

            if (lineClasses.TryGetValue(name.Text, out var newResult))
            {
                result = newResult;
            }
            else if (Parent != null)
            {
                result = Parent.FindLineClass(name, false);
            }

            if (!result.Found && warn)
            {
                if (FindObjectClass(name, false).Found)
                {
                    state.AddWarning(name, ErrorKind.Type, "class {0} can only be used for objects, not lines", name);
                }
                else
                {
                    state.AddWarning(name, ErrorKind.Reference, $"class {{0}} is not defined in scope `{RecursiveName()}`", name);
                }
            }

            return result;
        }

        public IR.Node? FindObject(Parse.Identifier name)
        {
            if (!canonicalObjects.TryGetValue(name.Text, out var result) && !bubbledObjects.TryGetValue(name.Text, out result) && Parent != null)
            {
                result = Parent.FindObject(name);
            }

            if (result == null)
            {
                state.AddWarning(name, ErrorKind.Reference, $"object {{0}} is not defined in scope `{RecursiveName()}`", name);
            }

            return result;
        }

        public void AddObjectClass(string key, ObjectContent objectClass)
        {
            objectClasses[key] = objectClass;
        }

        public void AddLineClass(string key, LineContent lineClass)
        {
            lineClasses[key] = lineClass;
        }

        public void AddObject(Parse.Identifier? key, IR.Node value)
        {
            if (key?.Text is string name && canonicalObjects.ContainsKey(name))
            {
                state.AddError(key, ErrorKind.Reference, $"object {{0}} has already been defined in scope `{RecursiveName()}`", key);
            }
            else
            {
                AddObject(key?.Text ?? Guid.NewGuid().ToString(), value, true);
            }
        }

        private void AddObject(string key, IR.Node value, bool canon)
        {
            if (Parent != null && !Parent.bubbledObjects.ContainsKey(key))
            {
                Parent.AddObject(key, value, false);
            }

            (canon ? canonicalObjects : bubbledObjects).Add(key, value);
        }

        private string RecursiveName()
        {
            var builder = new StringBuilder();

            if (Parent != null)
            {
                builder.Append(Parent.RecursiveName());
                builder.Append("::");
            }

            builder.Append(name);

            return builder.ToString();
        }

        public override string ToString()
        {
            return $"Scope {RecursiveName()}: {canonicalObjects.Count} objects";
        }
    }
}
