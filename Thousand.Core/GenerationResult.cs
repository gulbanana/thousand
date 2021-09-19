namespace Thousand
{
    public record GenerationResult<T>(T Diagram, GenerationError[] Warnings);
}
