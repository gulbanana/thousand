using Superpower.Model;

namespace Thousand.Parse
{
    internal interface ILocated
    {
        TextSpan Span { get; set; }
    }
}
