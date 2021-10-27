using System.Diagnostics.CodeAnalysis;
using Thousand.Parse;
using Thousand.API;

namespace Thousand.Tests
{
    static class Facade
    {
        public static bool TryParse(string text, GenerationState state, [NotNullWhen(true)] out AST.TypedDocument? outputSyntax)
        {
            if (!Preprocessor.TryPreprocess(state, text, out var syntax))
            {
                outputSyntax = null;
                return false;
            }

            if (!Typechecker.TryTypecheck(new Metadata(), state, syntax, allowErrors: false, out outputSyntax))
            {
                return false;
            }

            return true;
        }
    }
}
