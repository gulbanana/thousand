namespace Thousand.Compose
{
    internal record Track(decimal Start, decimal Center, decimal End)
    {
        public decimal Size => End - Start;
    }
}
