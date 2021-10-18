using Superpower;
using Superpower.Model;
using Superpower.Parsers;
using System;
using System.Linq;
using static Superpower.Parse;

namespace Thousand.Parse
{
    /**************************************************************************************
     * The tolerant AST may contain errors. It's split into 'lines' (technically scopes), *
     * and sufficiently-overridden to allow errors inside nested scopes.                  *
     **************************************************************************************/
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

        /*********************************************************************
         * Classes, which are actually better-typed than in the main parser! *
         *********************************************************************/

        public static TokenListParser<TokenKind, Macro<AST.TolerantObjectContent>[]> ClassContent =>
            from begin in Token.EqualTo(TokenKind.LeftBrace)
            from decs in Macro.Of(ObjectContent).ManyOptionalDelimited(terminator: TokenKind.RightBrace)
            from end in Token.EqualTo(TokenKind.RightBrace)
            select decs.ToArray();

        public static TokenListParser<TokenKind, AST.TolerantClass> Class { get; } =
            from keyword in Token.EqualTo(TokenKind.ClassKeyword)
            from name in Identifier.Any
            from arguments in Macro.Of(Untyped.ClassArgs.OptionalOrDefault(Array.Empty<AST.Argument>()))
            from bases in Token.EqualTo(TokenKind.Colon).IgnoreThen(Untyped.ClassCallList).OptionalOrDefault(Array.Empty<Macro<AST.ClassCall>>())
            from attrs in Shared.List(Untyped.UntypedAttribute).OptionalOrDefault(Array.Empty<AST.UntypedAttribute>())
            from children in ClassContent.OptionalOrDefault(Array.Empty<Macro<AST.TolerantObjectContent>>())
            select new AST.TolerantClass(name, arguments, bases, attrs, children);

        /*********************************************************************
         * Objects, which can be nested, containing declarations and errors. *
         *********************************************************************/

        public static TokenListParser<TokenKind, AST.TolerantObjectContent> ObjectContent { get; } = input =>
        {
            var first = input.ConsumeToken();
            if (!first.HasValue)
            {
                return InvalidDeclaration.Select(x => (AST.TolerantObjectContent)x)(input);
            }
            else if (first.Value.Kind == TokenKind.ClassKeyword)
            {
                return Class.Select(x => (AST.TolerantObjectContent)x)(input);
            }
            else if (first.Value.Kind == TokenKind.Identifier) // could be an attribute, an object or a line
            {
                var second = first.Remainder.ConsumeToken();

                if (!second.HasValue) // this is a trivial object!
                {
                    return Ref(() => Object!).Select(x => (AST.TolerantObjectContent)x)(input);
                }
                else if (second.Value.Kind == TokenKind.EqualsSign) // can only be an attribute
                {
                    return Shared.ObjectAttribute.Select(x => (AST.TolerantObjectContent)x)(input);
                }
                else // could still be an object or a line
                {
                    var classList = Untyped.ClassCallList(input);
                    if (classList.HasValue)
                    {
                        var identifier = classList.Remainder.ConsumeToken(); // object declaration or first object of line
                        if (!identifier.HasValue) // a slightly less trivial object 
                        {
                            return Ref(() => Object!).Select(x => (AST.TolerantObjectContent)x)(input);
                        }

                        var arrow = identifier.Remainder.ConsumeToken();
                        if (arrow.HasValue && arrow.Value.Kind is TokenKind.LeftArrow or TokenKind.RightArrow or TokenKind.NoArrow or TokenKind.DoubleArrow)
                        {
                            return Ref(() => Line!).Select(x => (AST.TolerantObjectContent)x)(input);
                        }
                        else
                        {
                            return Ref(() => Object!).Select(x => (AST.TolerantObjectContent)x)(input);
                        }
                    }
                    else
                    {
                        return InvalidDeclaration.Select(x => (AST.TolerantObjectContent)x)(input);
                    }
                }
            }
            else
            {
                return InvalidDeclaration.Select(x => (AST.TolerantObjectContent)x)(input);
            }
        };

        public static TokenListParser<TokenKind, AST.TolerantObject> Object { get; } =
            from classes in Untyped.ClassCallList
            from name in Shared.Target.AsNullable().OptionalOrDefault()
            from attrs in Shared.List(Macro.Of(Shared.ObjectAttribute)).OptionalOrDefault(Array.Empty<Macro<AST.ObjectAttribute>>())
            from children in Shared.Scope(Macro.Of(ObjectContent)).OptionalOrDefault(Array.Empty<Macro<AST.TolerantObjectContent>>())
            select new AST.TolerantObject(classes, name, attrs, children);

        /*************************************************************************
         * Lines, which only differ from their main variant by adding token info *
         *************************************************************************/
        public static TokenListParser<TokenKind, AST.TolerantLine> Line { get; } =
            from calls in Untyped.ClassCallList
            from chain in Shared.Edges
            from attrs in Shared.List(Macro.Of(Shared.SegmentAttribute)).OptionalOrDefault(Array.Empty<Macro<AST.SegmentAttribute>>())
            select new AST.TolerantLine(calls, chain.ToArray(), attrs);

        /**************************************************************
         * Documents, which contain top-level declarations and errors *
         **************************************************************/

        public static TokenListParser<TokenKind, AST.TolerantDocumentContent> DocumentContent { get; } = input =>
        {
            var first = input.ConsumeToken();
            if (first.Value.Kind == TokenKind.ClassKeyword) // could be a class declaration
            {
                return Class.Select(x => (AST.TolerantDocumentContent)x)(input);
            }
            else if (first.Value.Kind == TokenKind.Identifier) // could be an attribute, an object or a line
            {
                var second = first.Remainder.ConsumeToken();

                if (!second.HasValue) // this is a trivial object!
                {
                    return Object.Select(x => (AST.TolerantDocumentContent)x)(input);
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
                            return Object.Select(x => (AST.TolerantDocumentContent)x)(input);
                        }

                        var arrow = identifier.Remainder.ConsumeToken();
                        if (arrow.HasValue && arrow.Value.Kind is TokenKind.LeftArrow or TokenKind.RightArrow or TokenKind.NoArrow or TokenKind.DoubleArrow)
                        {
                            return Line.Select(x => (AST.TolerantDocumentContent)x)(input);
                        }
                        else
                        {
                            return Object.Select(x => (AST.TolerantDocumentContent)x)(input);
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
