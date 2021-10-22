namespace Thousand.Evaluate
{
    public record ObjectContent(bool Found, AST.ObjectAttribute[] Attributes, AST.TypedObjectContent[] Children);
    public record LineContent(bool Found, AST.LineAttribute[] Attributes);
}
