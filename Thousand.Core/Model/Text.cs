namespace Thousand.Model
{
    // at the moment this is just a wrapper which contains optional text; later it might become something like an attributed string
    public struct Text
    {
        public bool HasValue { get; }
        public string? Value { get; }

        public Text(string? value)
        {
            HasValue = true;
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
