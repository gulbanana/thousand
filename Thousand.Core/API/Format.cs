using System;
using System.Linq;

namespace Thousand.API
{
    static class Format
    {
        public static string Doc(string description, string type, UseKind kind, params string[] examples)
        {
            return $@"{description}

_Value_: {type}

_Applies to:_ {kind switch
            {
                UseKind.Object => "objects",
                UseKind.Line => "lines",
                UseKind.Document => "the whole diagram",
                UseKind.Region => "objects or the whole diagram",
                UseKind.Entity => "objects or lines"
            }}

_Examples:_ {string.Join(", ", examples.Select(e => $"`{e}`"))}";
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
