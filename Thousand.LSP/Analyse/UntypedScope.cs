﻿using System;
using System.Collections.Generic;
using Thousand.Model;

namespace Thousand.LSP.Analyse
{
    // this duplicates the algorithm (but not the structure) used by Evaluate.TypedScope
    // there's an irritating tradeoff here between the batch-compilation model and error tolerance
    public sealed class UntypedScope
    {
        private readonly string name;
        private readonly List<UntypedScope> children = new();
        public UntypedScope? Parent { get; private init; }
        public Dictionary<string, AST.UntypedClass> Classes { get; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, AST.UntypedObject> Objects { get; } = new(StringComparer.OrdinalIgnoreCase);

        public UntypedScope(string name)
        {
            this.name = name;
        }

        public UntypedScope Push(string name)
        {
            var result = new UntypedScope(name) { Parent = this };
            children.Add(result);
            return result;
        }

        public void Pop(AST.UntypedClass c)
        {
            Classes[c.Name.AsKey] = c;
            Pop();
        }

        public void Pop(AST.UntypedObject o)
        {
            if (o.Name != null && !Objects.ContainsKey(o.Name.AsKey))
            {
                Objects.Add(o.Name.AsKey, o);
            }
            Pop();
        }

        private void Pop()
        {
            foreach (var child in children)
            {
                foreach (var o in child.Objects)
                {
                    if (!Objects.ContainsKey(o.Key))
                    {
                        Objects[o.Key] = o.Value;
                    }
                }
            }
        }

        public AST.UntypedObject? FindObject(Name name)
        {
            if (Objects.ContainsKey(name.AsKey))
            {
                return Objects[name.AsKey];
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

        public AST.UntypedClass? FindClass(Name name)
        {
            if (Classes.ContainsKey(name.AsKey))
            {
                return Classes[name.AsKey];
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

        public override string ToString()
        {
            return (Parent == null ? "" : Parent.ToString() + "::") + name;
        }
    }
}
