using Superpower;
using Superpower.Model;
using System.Linq;

namespace Thousand.Parse
{
    public static class Tolerant
    {
        public static TokenListParser<TokenKind, AST.InvalidDeclaration> InvalidDeclaration { get; } = input =>
        {
            var remainder = input;
            while (!remainder.IsAtEnd)
            {
                var next = remainder.ConsumeToken();
                remainder = next.Remainder;
                if (!next.HasValue || next.Value.Kind == TokenKind.LineSeparator)
                {
                    break;
                }
            }

            return TokenListParserResult.Value(new AST.InvalidDeclaration(), input, remainder);
        };

        public static TokenListParser<TokenKind, AST.TolerantDocumentContent> DocumentContent { get; } = input =>
        {
            var first = input.ConsumeToken();
            if (first.Value.Kind == TokenKind.ClassKeyword) // could be a class declaration
            {
                return Untyped.Class.Select(x => (AST.TolerantDocumentContent)x)(input);
            }
            else if (first.Value.Kind == TokenKind.Identifier) // could be an attribute, an object or a line
            {
                var second = first.Remainder.ConsumeToken();

                if (!second.HasValue) // this is a trivial object!
                {
                    return Untyped.Object.Select(x => (AST.TolerantDocumentContent)x)(input);
                }
                if (second.Value.Kind == TokenKind.EqualsSign) // can only be an attribute
                {
                    return Shared.DiagramAttribute.Select(x => (AST.TolerantDocumentContent)x)(input);
                }
                else // could still be an object or a line
                {
                    var classList = Untyped.ClassCallList(input);
                    if (classList.HasValue)
                    {
                        var identifier = classList.Remainder.ConsumeToken(); // object declaration or first object of line
                        if (!identifier.HasValue) // a slightly less trivial object 
                        {
                            return Untyped.Object.Select(x => (AST.TolerantDocumentContent)x)(input);
                        }

                        var arrow = identifier.Remainder.ConsumeToken();
                        if (arrow.HasValue && arrow.Value.Kind is TokenKind.LeftArrow or TokenKind.RightArrow or TokenKind.NoArrow or TokenKind.DoubleArrow)
                        {
                            return Untyped.Line.Select(x => (AST.TolerantDocumentContent)x)(input);
                        }
                        else
                        {
                            return Untyped.Object.Select(x => (AST.TolerantDocumentContent)x)(input);
                        }
                    }
                    else
                    {
                        return InvalidDeclaration.Select(x => (AST.TolerantDocumentContent)x)(input);
                    }
                }
            }
            else
            {
                return InvalidDeclaration.Select(x => (AST.TolerantDocumentContent)x)(input);
            }
        };

        public static TokenListParser<TokenKind, AST.TolerantDocument> Document { get; } =
            Macro.Of(DocumentContent)
                .ManyOptionalDelimited()
                .Select(decs => new AST.TolerantDocument(decs.ToArray()))
                .AtEnd();
    }
}
