using System.Collections.Generic;
using System.Linq;

namespace Thousand.Tests
{
    internal static class Extensions
    {
        public static IEnumerable<IR.Node> WalkNodes(this IR.Region self)
        {
            foreach (var obj in self.Entities.OfType<IR.Node>())
            {
                yield return obj;
                foreach (var child in obj.Region.WalkNodes())
                {
                    yield return child;
                }
            }
        }

        public static IEnumerable<Layout.Command> WalkCommands(this Layout.Diagram self)
        {
            foreach (var cmd in self.Commands)
            {
                yield return cmd;

                if (cmd is Layout.Transform transform)
                {
                    foreach (var child in transform.WalkCommands())
                    {
                        yield return child;
                    }
                }
            }
        }

        public static IEnumerable<Layout.Command> WalkCommands(this Layout.Transform self)
        {
            foreach (var cmd in self.Commands)
            {
                yield return cmd;

                if (cmd is Layout.Transform transform)
                {
                    foreach (var child in transform.WalkCommands())
                    {
                        yield return child;
                    }
                }
            }
        }
    }
}
