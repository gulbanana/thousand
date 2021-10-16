using System;
using System.Collections.Generic;
using System.Text;

namespace Thousand.Evaluate
{
    internal sealed class Scope
    {
        private readonly string name;
        private readonly GenerationState state;
        public Scope? Parent { get; private init; }

        private Dictionary<string, IR.Object> canonicalObjects { get; } = new(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, IR.Object> bubbledObjects { get; } = new(StringComparer.OrdinalIgnoreCase);
        public IReadOnlyDictionary<string, IR.Object> Objects => canonicalObjects;

        public Scope(string name, GenerationState state)
        {
            this.name = name;
            this.state = state;
        }

        public Scope Chain(string name)
        {
            return new Scope(name, state) { Parent = this };
        }

        public void AddObject(Parse.Identifier? key, IR.Object value)
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

        private void AddObject(string key, IR.Object value, bool canon)
        {
            if (Parent != null && !Parent.bubbledObjects.ContainsKey(key))
            {
                Parent.AddObject(key, value, false);
            }

            (canon ? canonicalObjects : bubbledObjects).Add(key, value);
        }

        public IR.Object? FindObject(Parse.Identifier name)
        {
            var result = default(IR.Object?);

            if (!canonicalObjects.TryGetValue(name.Text, out result) && !bubbledObjects.TryGetValue(name.Text, out result) && Parent != null)
            {
                result = Parent.FindObject(name);
            }

            if (result == null)
            {
                state.AddWarning(name, ErrorKind.Reference, $"object {{0}} is not defined in scope `{RecursiveName()}`", name);
            }

            return result;
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
            return $"Scope {RecursiveName()}: {Objects.Count} objects";
        }
    }
}
