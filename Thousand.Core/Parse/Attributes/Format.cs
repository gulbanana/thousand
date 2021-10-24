using System;
using System.Linq;
using System.Text;

namespace Thousand.Parse.Attributes
{
    internal static class Format
    {
        public static string Doc(string? description, string type, UseKind kind, params string[] examples)
        {
            var builder = new StringBuilder();

            if (description != null)
            {
                builder.Append($@"{description}

");
            }

            builder.Append(@$"_Value_: {type}

_Applies to:_ {kind switch
            {
                UseKind.Object => "objects",
                UseKind.Line => "lines",
                UseKind.Document => "the whole diagram",
                UseKind.Region => "objects or the whole diagram",
                UseKind.Entity => "objects or lines"
            }}

_Examples:_ {string.Join(", ", examples.Select(e => $"`{e}`"))}");

            return builder.ToString();
        }

        public static string Doc(string type, UseKind kind, params string[] examples) => Doc(null, type, kind, examples);

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
