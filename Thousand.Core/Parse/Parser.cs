using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Thousand.Parse
{
    public class Parser
    {
        public static bool TryParse(string text, List<GenerationError> warnings, List<GenerationError> errors, [NotNullWhen(true)] out AST.Document? document)
        {
            var tokenizer = Tokenizer.Build();

            var tokenized = tokenizer.TryTokenize(text);
            if (!tokenized.HasValue)
            {
                errors.Add(new(tokenized.ErrorPosition, ErrorKind.Syntax, tokenized.FormatErrorMessageFragment()));
                document = null;
                return false;
            }

            var parsed = TokenParsers.Document(tokenized.Value);
            if (!parsed.HasValue)
            {
                errors.Add(new(parsed.ErrorPosition, ErrorKind.Syntax, parsed.FormatErrorMessageFragment()));
                document = null;
                return false;
            }

            document = parsed.Value;
            return true;
        }
    }
}
