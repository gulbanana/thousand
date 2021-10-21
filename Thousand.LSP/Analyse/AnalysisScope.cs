using System;
using System.Collections.Generic;
using Thousand.Parse;

namespace Thousand.LSP.Analyse
{
    // XXX this duplicates the algorithm (but not the structure) used by Evaluate.Scope
    // there's an irritating tradeoff here between the batch-compilation model and error tolerance
    sealed class AnalysisScope
    {
        private readonly List<AnalysisScope> children = new();
        public AnalysisScope? Parent { get; private init; }
        public Dictionary<string, AST.UntypedClass> Classes { get; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, AST.UntypedObject> Objects { get; } = new(StringComparer.OrdinalIgnoreCase);

        public AnalysisScope Push()
        {
            var result = new AnalysisScope { Parent = this };
            children.Add(result);
            return result;
        }

        public void Pop(AST.UntypedClass c)
        {
            Classes.Add(c.Name.Text, c);
            Pop();
        }

        public void Pop(AST.UntypedObject o)
        {
            if (o.Name != null && !Objects.ContainsKey(o.Name.Text))
            {
                Objects.Add(o.Name.Text, o);
            }
            Pop();
        }

        private void Pop()
        {
            foreach (var child in children)
            {
                foreach (var c in child.Classes)
                {
                    if (!Classes.ContainsKey(c.Key))
                    {
                        Classes[c.Key] = c.Value;
                    }
                }

                foreach (var o in child.Objects)
                {
                    if (!Objects.ContainsKey(o.Key))
                    {
                        Objects[o.Key] = o.Value;
                    }
                }
            }
        }

        public AST.UntypedObject? FindObject(Identifier name)
        {
            if (Objects.ContainsKey(name.Text))
            {
                return Objects[name.Text];
            }
            else if (Parent != null)
            {
                return Parent.FindObject(name);
            }
            else
            {
                return null;
            }
        }

        public AST.UntypedClass? FindClass(Identifier name)
        {
            if (Classes.ContainsKey(name.Text))
            {
                return Classes[name.Text];
            }
            else if (Parent != null)
            {
                return Parent.FindClass(name);
            }
            else
            {
                return null;
            }
        }
    }
}
