namespace Thousand.Evaluate
{
    public record ObjectContent(bool Found, AST.ObjectAttribute[] Attributes, AST.TypedDeclaration[] Children);
    public record LineContent(bool Found, AST.LineAttribute[] Attributes);
}
