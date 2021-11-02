namespace Thousand.Model
{
    public record Shape(ShapeKind Style, decimal CornerRadius)
    {
        public Shape(ShapeKind Style) : this(Style, 0m) { }
    }
}
