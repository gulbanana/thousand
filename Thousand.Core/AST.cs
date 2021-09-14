namespace Thousand.AST
{
    public record Node(string Label);
    public record Document(Node[] Nodes);
}
