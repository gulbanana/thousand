using Superpower.Model;

namespace Thousand.Parse
{
    internal interface ILocated
    {
        TextSpan Location { get; set; }
    }
}
