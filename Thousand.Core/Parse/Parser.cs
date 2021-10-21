using System.Diagnostics.CodeAnalysis;

namespace Thousand.Parse
{
    public sealed class Parser
    {
        public static bool TryParse(string text, GenerationState state, [NotNullWhen(true)] out AST.TypedDocument? outputSyntax)
        {
            if (!Preprocessor.TryPreprocess(state, text, out var t))
            {
                outputSyntax = null;
                return false;
            }

            if (!Typechecker.TryTypecheck(state, t.Value.tokens, t.Value.syntax, stripErrors: false, out outputSyntax))
            {
                return false;
            }

            return true;
        }
    }
}
