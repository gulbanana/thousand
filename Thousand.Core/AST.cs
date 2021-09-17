namespace Thousand.AST
{
    public record Attribute(string Key, string Value);
    public record Node(string Label, Attribute[] Attributes);
    public record Document(Node[] Nodes);
}
