using System;
using System.Collections.Generic;
using System.Text;
using Thousand.Model;

namespace Thousand.Evaluate
{
    internal sealed class TypedScope
    {
        private readonly int index;
        private readonly string name;
        private readonly GenerationState state;
        public TypedScope? Parent { get; private init; }
        private int children;

        private readonly Dictionary<string, IR.Node> canonicalObjects = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, IR.Node> bubbledObjects = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, ObjectContent> objectClasses = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, LineContent> lineClasses = new(StringComparer.OrdinalIgnoreCase);

        private TypedScope(int index, string name, GenerationState state)
        {
            this.index = index;
            this.name = name;
            this.state = state;
            children = 0;
        }

        public TypedScope(string name, GenerationState state) : this(-1, name, state) { }

        public TypedScope Chain(string name, bool local)
        {
            return new TypedScope(local ? children++ : -1, name, state) { Parent = this };
        }

        public bool HasRequiredClass(Name b)
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

        public ObjectContent FindObjectClass(Name name, bool warn)
        {
            var result = new ObjectContent(false, Array.Empty<AST.ObjectAttribute>(), Array.Empty<AST.TypedDeclaration>());

            if (objectClasses.TryGetValue(name.AsKey, out var newResult))
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

        public LineContent FindLineClass(Name name, bool warn)
        {
            var result = new LineContent(false, Array.Empty<AST.LineAttribute>());

            if (lineClasses.TryGetValue(name.AsKey, out var newResult))
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

        public IR.Node? FindObject(Name name)
        {
            if (!canonicalObjects.TryGetValue(name.AsKey, out var result) && !bubbledObjects.TryGetValue(name.AsKey, out result) && Parent != null)
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

        public void AddObject(Name? key, IR.Node value)
        {
            if (key?.AsKey is string name && canonicalObjects.ContainsKey(name))
            {
                state.AddError(key, ErrorKind.Reference, $"object {{0}} has already been defined in scope `{RecursiveName()}`", key);
            }
            else
            {
                AddObject(key?.AsKey ?? Guid.NewGuid().ToString(), value, true);
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

        private string RecursiveObjectName()
        {
            var builder = new StringBuilder();

            if (Parent != null)
            {
                builder.Append(Parent.RecursiveObjectName());
            }

            if (index != -1)
            {
                builder.Append('_');
                builder.Append(index);
            }

            return builder.ToString();
        }

        public override string ToString()
        {
            return $"Scope {RecursiveName()}: {canonicalObjects.Count} objects";
        }

        public string UniqueName()
        {
            return $"O{RecursiveObjectName()}";
        }
    }
}
