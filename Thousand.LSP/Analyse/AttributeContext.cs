namespace Thousand.LSP.Analyse
{
    public record AttributeContext(AST.UntypedAttribute Syntax, ParentKind ParentKind, string[] ParentAttributes);
}
