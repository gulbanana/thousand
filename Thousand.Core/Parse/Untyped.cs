using Superpower;
using Superpower.Model;
using Superpower.Parsers;
using System;
using System.Linq;
using static Superpower.Parse;

namespace Thousand.Parse
{
    public static class Untyped
    {
        public static TokenListParser<TokenKind, AST.UntypedAttribute> UntypedAttribute { get; } =
            from key in Identifier.Any.Named("attribute name")
            from _ in Token.EqualTo(TokenKind.EqualsSign)
            from value in Macro.Raw(TokenKind.Comma, TokenKind.RightBracket)
            select new AST.UntypedAttribute(key, value);

        /**************************************************
         * Class calls - invocations of template classes. *
         **************************************************/

        public static TokenListParser<TokenKind, Macro[]> CallArgs { get; } =
            from begin in Token.EqualTo(TokenKind.LeftParenthesis)
            from arguments in Macro.Raw(TokenKind.Comma, TokenKind.RightParenthesis).AtLeastOnceDelimitedBy(Token.EqualTo(TokenKind.Comma))
            from end in Token.EqualTo(TokenKind.RightParenthesis)
            select arguments;

        public static TokenListParser<TokenKind, AST.ClassCall> ClassCall { get; } =
            from name in Identifier.Any
            from arguments in CallArgs.OptionalOrDefault(Array.Empty<Macro>())
            select new AST.ClassCall(name, arguments);

        public static TokenListParser<TokenKind, Macro<AST.ClassCall>[]> ClassCallList { get; } =
            Macro.Of(ClassCall).AtLeastOnceDelimitedBy(Token.EqualTo(TokenKind.Period));

        /*************************************************************
         * Classes which may be templates requiring macro-expansion. *
         *************************************************************/

        public static TokenListParser<TokenKind, AST.Argument> ClassArg { get; } =
            from name in Identifier.Variable
            from @default in Token.EqualTo(TokenKind.EqualsSign).IgnoreThen(Macro.Raw(TokenKind.Comma, TokenKind.RightParenthesis)).AsNullable().OptionalOrDefault()
            select new AST.Argument(name, @default);

        public static TokenListParser<TokenKind, AST.Argument[]> ClassArgs { get; } =
            from begin in Token.EqualTo(TokenKind.LeftParenthesis)
            from arguments in ClassArg.AtLeastOnceDelimitedBy(Token.EqualTo(TokenKind.Comma))
            from end in Token.EqualTo(TokenKind.RightParenthesis)
            select arguments;

        public static TokenListParser<TokenKind, Macro[]> ClassContent =>
            from begin in Token.EqualTo(TokenKind.LeftBrace)
            from decs in Macro.Of(ObjectContent).Cast<Macro<AST.UntypedObjectContent>, Macro>().Or(Macro.Raw(TokenKind.LineSeparator, TokenKind.RightBrace)).ManyOptionalDelimited(terminator: TokenKind.RightBrace)
            from end in Token.EqualTo(TokenKind.RightBrace)
            select decs.ToArray();

        public static TokenListParser<TokenKind, AST.UntypedClass> Class { get; } =
            from keyword in Token.EqualTo(TokenKind.ClassKeyword)
            from name in Identifier.Any
            from arguments in Macro.Of(ClassArgs.OptionalOrDefault(Array.Empty<AST.Argument>()))
            from bases in Token.EqualTo(TokenKind.Colon).IgnoreThen(ClassCallList).OptionalOrDefault(Array.Empty<Macro<AST.ClassCall>>())
            from attrs in Shared.List(UntypedAttribute).OptionalOrDefault(Array.Empty<AST.UntypedAttribute>())
            from children in ClassContent.OptionalOrDefault(Array.Empty<Macro>())
            select new AST.UntypedClass(name, arguments, bases, attrs, children);

        /*************************************************************************************
         * Objects and lines, which can instantiate template classes with a macro expansion. *
         *************************************************************************************/

        public static TokenListParser<TokenKind, AST.UntypedLine> Line { get; } =
            from calls in ClassCallList
            from chain in Shared.Edges
            from attrs in Shared.List(Shared.SegmentAttribute).OptionalOrDefault(Array.Empty<AST.SegmentAttribute>())
            select new AST.UntypedLine(calls, chain.ToArray(), attrs);

        public static TokenListParser<TokenKind, AST.UntypedObjectContent> ObjectContent { get; } = input =>
        {
            var fail = TokenListParserResult.Empty<TokenKind, AST.UntypedObjectContent>(input, new[] { "attribute", "class", "object", "line" });

            var first = input.ConsumeToken();
            if (!first.HasValue)
            {
                return fail;
            }
            else if (first.Value.Kind == TokenKind.ClassKeyword)
            {
                return Class.Select(x => (AST.UntypedObjectContent)x)(input);
            }
            else if (first.Value.Kind == TokenKind.Identifier) // could be an attribute, an object or a line
            {
                var second = first.Remainder.ConsumeToken();

                if (!second.HasValue) // this is a trivial object!
                {
                    return Ref(() => Object!).Select(x => (AST.UntypedObjectContent)x)(input);
                }
                else if (second.Value.Kind == TokenKind.EqualsSign) // can only be an attribute
                {
                    return Shared.ObjectAttribute.Select(x => (AST.UntypedObjectContent)x)(input);
                }
                else // could still be an object or a line
                {
                    var classList = ClassCallList(input);
                    if (classList.HasValue)
                    {
                        var identifier = classList.Remainder.ConsumeToken(); // object declaration or first object of line
                        if (!identifier.HasValue) // a slightly less trivial object 
                        {
                            return Ref(() => Object!).Select(x => (AST.UntypedObjectContent)x)(input);
                        }

                        var arrow = identifier.Remainder.ConsumeToken();
                        if (arrow.HasValue && arrow.Value.Kind is TokenKind.LeftArrow or TokenKind.RightArrow or TokenKind.NoArrow or TokenKind.DoubleArrow)
                        {
                            return Line.Select(x => (AST.UntypedObjectContent)x)(input);
                        }
                        else
                        {
                            return Ref(() => Object!).Select(x => (AST.UntypedObjectContent)x)(input);
                        }
                    }
                    else
                    {
                        return fail;
                    }
                }
            }
            else
            {
                return fail;
            }
        };

        public static TokenListParser<TokenKind, AST.UntypedObject> Object { get; } =
            from classes in ClassCallList
            from name in Shared.Target.AsNullable().OptionalOrDefault()
            from attrs in Shared.List(Shared.ObjectAttribute).OptionalOrDefault(Array.Empty<AST.ObjectAttribute>())
            from children in Shared.Scope(ObjectContent).OptionalOrDefault(Array.Empty<AST.UntypedObjectContent>())
            select new AST.UntypedObject(classes, name, attrs, children);

        /*****************************************************************
         * Documents which require macro resolution, currently hermetic. *
         *****************************************************************/

        public static TokenListParser<TokenKind, AST.UntypedDocumentContent> DocumentContent { get; } = input =>
        {
            var fail = TokenListParserResult.Empty<TokenKind, AST.UntypedDocumentContent>(input, new[] { "attribute", "class", "object", "line" });

            var first = input.ConsumeToken();
            if (first.Value.Kind == TokenKind.ClassKeyword) // could be a class declaration
            {
                return Macro.Of(Class).Select(x => (AST.UntypedDocumentContent)x)(input);
            }
            else if (first.Value.Kind == TokenKind.Identifier) // could be an attribute, an object or a line
            {
                var second = first.Remainder.ConsumeToken();

                if (!second.HasValue) // this is a trivial object!
                {
                    return Object.Select(x => (AST.UntypedDocumentContent)x)(input);
                }
                if (second.Value.Kind == TokenKind.EqualsSign) // can only be an attribute
                {
                    return Shared.DiagramAttribute.Select(x => (AST.UntypedDocumentContent)x)(input);
                }
                else // could still be an object or a line
                {
                    var classList = ClassCallList(input);
                    if (classList.HasValue)
                    {
                        var identifier = classList.Remainder.ConsumeToken(); // object declaration or first object of line
                        if (!identifier.HasValue) // a slightly less trivial object 
                        {
                            return Object.Select(x => (AST.UntypedDocumentContent)x)(input);
                        }

                        var arrow = identifier.Remainder.ConsumeToken();
                        if (arrow.HasValue && arrow.Value.Kind is TokenKind.LeftArrow or TokenKind.RightArrow or TokenKind.NoArrow or TokenKind.DoubleArrow)
                        {
                            return Line.Select(x => (AST.UntypedDocumentContent)x)(input);
                        }
                        else
                        {
                            return Object.Select(x => (AST.UntypedDocumentContent)x)(input);
                        }
                    }
                    else
                    {
                        return fail;
                    }
                }
            }
            else
            {
                return fail;
            }
        };

        public static TokenListParser<TokenKind, AST.UntypedDocument> Document { get; } =
            DocumentContent
                .ManyOptionalDelimited()
                .Select(decs => new AST.UntypedDocument(decs.ToArray()))
                .AtEnd();
    }
}
