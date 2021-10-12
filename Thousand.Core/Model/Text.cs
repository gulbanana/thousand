using System.Diagnostics.CodeAnalysis;

namespace Thousand.Model
{
    // at the moment this is just a wrapper which can optionally contain text; later it might become something like an attributed string
    public struct Text
    {
        public bool HasValue { [MemberNotNullWhen(true, nameof(Value))] get; }
        public string? Value { get; }

        public Text(string? value)
        {
            HasValue = value != null;
            Value = value;
        }

        public override bool Equals(object? obj)
        {
            return obj is Text other && other.HasValue == this.HasValue && other.Value == this.Value;
        }

        public override int GetHashCode()
        {
            return Value?.GetHashCode() ?? 0;
        }
    }
}
