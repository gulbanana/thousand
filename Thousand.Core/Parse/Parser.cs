using System.Diagnostics.CodeAnalysis;

namespace Thousand.Parse
{
    public sealed class Parser
    {
        public static bool TryParse(string text, GenerationState state, [NotNullWhen(true)] out AST.TypedDocument? outputSyntax)
        {
            if (!Preprocessor.TryPreprocess(state, text, out var syntax))
            {
                outputSyntax = null;
                return false;
            }

            if (!Typechecker.TryTypecheck(state, syntax, allowErrors: false, out outputSyntax))
            {
                return false;
            }

            return true;
        }
    }
}
