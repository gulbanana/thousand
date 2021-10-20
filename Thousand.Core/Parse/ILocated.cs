using Superpower.Model;

namespace Thousand.Parse
{
    public interface ILocated
    {
        TextSpan Span { get; set; }
    }
}
