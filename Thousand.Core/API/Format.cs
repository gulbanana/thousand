using System;
using System.Linq;
using System.Text;

namespace Thousand.API
{
    static class Format
    {
        public static string Doc(string description, string type, UseKind? kind, params string[] examples)
        {
            var builder = new StringBuilder();
            builder.Append($@"{description}

_Value_: {type}");

            if (kind.HasValue)
            {
                builder.Append($@"

_Applies to:_ {kind.Value switch
                {
                    UseKind.Object => "objects",
                    UseKind.Line => "lines",
                    UseKind.Document => "the whole diagram",
                    UseKind.Region => "objects or the whole diagram",
                    UseKind.Entity => "objects or lines"
                }}");
            }

            if (examples.Length > 0)
            {
                builder.Append($@"

_Examples:_ {string.Join(", ", examples.Select(e => $"`{e}`"))}");
            }

            return builder.ToString();
        }

        public static string Names<T>() where T : struct, Enum
        {
            return string.Join(", ", Enum.GetNames<T>().Select(n => $"`{n.ToLowerInvariant()}`"));
        }

        public static string NamesOrNone<T>() where T : struct, Enum
        {
            return string.Join(", ", Enum.GetNames<T>().Select(n => $"`{n.ToLowerInvariant()}`")) + " or `none`";
        }
    }
}
