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

        public static TokenListParser<TokenKind, Parse.Macro<Parse.Identifier>[]> ClassList { get; } =
            Macro.Of(Identifier.Any).AtLeastOnceDelimitedBy(Token.EqualTo(TokenKind.Period));

        /*************************************************************
         * Classes which may be templates requiring macro-expansion. *
         *************************************************************/

        public static TokenListParser<TokenKind, Identifier[]> ClassArgs { get; } =
            from begin in Token.EqualTo(TokenKind.LeftParenthesis)
            from arguments in Identifier.Variable.AtLeastOnceDelimitedBy(Token.EqualTo(TokenKind.Comma))
            from end in Token.EqualTo(TokenKind.RightParenthesis)
            select arguments;

        public static TokenListParser<TokenKind, AST.UntypedClass> Class { get; } =
            from keyword in Token.EqualTo(TokenKind.ClassKeyword)
            from name in Identifier.Any
            from arguments in Macro.Of(ClassArgs)
            from bases in Shared.BaseClasses
            from attrs in Shared.List(UntypedAttribute).OptionalOrDefault(Array.Empty<AST.UntypedAttribute>())
            select new AST.UntypedClass(name, arguments, bases, attrs);

        /**************************************************
         * Class calls - invocations of template classes. *
         **************************************************/

        public static TokenListParser<TokenKind, Macro[]> ClassCall { get; } =
            from begin in Token.EqualTo(TokenKind.LeftParenthesis)
            from arguments in Macro.Raw(TokenKind.Comma, TokenKind.RightParenthesis).AtLeastOnceDelimitedBy(Token.EqualTo(TokenKind.Comma))
            from end in Token.EqualTo(TokenKind.RightParenthesis)
            select arguments;

        public static TokenListParser<TokenKind, Macro<Macro[]>?> OptionalClassCall { get; } =
            Macro.Of(ClassCall).AsNullable().OptionalOrDefault(null);

        /*************************************************************************************
         * Objects and lines, which can instantiate template classes with a macro expansion. *
         *************************************************************************************/

        public static TokenListParser<TokenKind, AST.UntypedLine> Line { get; } =
            from classes in ClassList
            from call in OptionalClassCall
            from chain in Shared.Edges
            from attrs in Shared.List(Shared.SegmentAttribute).OptionalOrDefault(Array.Empty<AST.SegmentAttribute>())
            select new AST.UntypedLine(classes, call, chain.ToArray(), attrs);

        public static TokenListParser<TokenKind, AST.UntypedObjectContent> ObjectContent { get; } = input =>
        {
            var first = input.ConsumeToken();
            if (first.Value.Kind == TokenKind.Identifier) // this is part of a classlist. could be an attribute, an object or a line
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
                    var classList = ClassList(input);
                    if (classList.HasValue)
                    {
                        var third = classList.Remainder.ConsumeToken();
                        if (!third.HasValue) // a slightly less trivial object
                        {
                            return Ref(() => Object!).Select(x => (AST.UntypedObjectContent)x)(input);
                        }

                        if (third.Value.Kind is TokenKind.Identifier or TokenKind.String) // line or named object; followed immediately by an arrow if line
                        {
                            var arrow = third.Remainder.ConsumeToken();
                            if (arrow.HasValue && arrow.Value.Kind is TokenKind.LeftArrow or TokenKind.RightArrow or TokenKind.NoArrow or TokenKind.DoubleArrow)
                            {
                                return Line.Select(x => (AST.UntypedObjectContent)x)(input);
                            }
                            else
                            {
                                return Ref(() => Object!).Select(x => (AST.UntypedObjectContent)x)(input);
                            }
                        }
                        else if (third.Value.Kind == TokenKind.LeftParenthesis) // line or anonymous object; followed by a call then a target if line
                        {
                            var call = ClassCall(classList.Remainder);
                            if (call.HasValue)
                            {
                                var lineFrom = Shared.Target(call.Remainder);
                                if (lineFrom.HasValue)
                                {
                                    return Line.Select(x => (AST.UntypedObjectContent)x)(input);
                                }
                                else
                                {
                                    return Ref(() => Object!).Select(x => (AST.UntypedObjectContent)x)(input);
                                }
                            }
                        }
                        else // still a (slightly less trivial) object
                        {
                            return Ref(() => Object!).Select(x => (AST.UntypedObjectContent)x)(input);
                        }
                    }
                }
            }

            return TokenListParserResult.Empty<TokenKind, AST.UntypedObjectContent>(input, new[] { "attribute", "object", "line" });
        };

        public static TokenListParser<TokenKind, AST.UntypedObject> Object { get; } =
            from classes in ClassList
            from name in Shared.Target.AsNullable().OptionalOrDefault()
            from call in OptionalClassCall
            from attrs in Shared.List(Shared.ObjectAttribute).OptionalOrDefault(Array.Empty<AST.ObjectAttribute>())
            from children in Shared.Scope(ObjectContent).OptionalOrDefault(Array.Empty<AST.UntypedObjectContent>())
            select new AST.UntypedObject(classes, name, call, attrs, children);

        /*****************************************************************
         * Documents which require macro resolution, currently hermetic. *
         *****************************************************************/

        public static TokenListParser<TokenKind, AST.UntypedDocumentContent> DocumentContent { get; } = input =>
        {
            var first = input.ConsumeToken();
            if (first.Value.Kind == TokenKind.ClassKeyword) // could be a class declaration
            {
                var untypedClass = Macro.Of(Class).Select(x => (AST.UntypedDocumentContent)x)(input);
                if (untypedClass.HasValue)
                {
                    return untypedClass;
                }
                else
                {
                    return Typed.Class.Select(x => (AST.UntypedDocumentContent)x)(input); // we support untemplated classes in the template AST as an error-quality optimisation
                }
            }
            else if (first.Value.Kind == TokenKind.Identifier) // this is part of a classlist. could be an attribute, an object or a line
            {
                var second = first.Remainder.ConsumeToken();

                if (!second.HasValue) // this is a trivial object!
                {
                    return Object.Select(x => (AST.UntypedDocumentContent)x)(input);
                }
                else if (second.Value.Kind == TokenKind.EqualsSign) // can only be an attribute
                {
                    return Shared.DiagramAttribute.Select(x => (AST.UntypedDocumentContent)x)(input);
                }
                else // could still be an object or a line
                {
                    var classList = ClassList(input);
                    if (classList.HasValue)
                    {
                        var third = classList.Remainder.ConsumeToken();
                        if (!third.HasValue) // a slightly less trivial object
                        {
                            return Object.Select(x => (AST.UntypedDocumentContent)x)(input);
                        }

                        if (third.Value.Kind is TokenKind.Identifier or TokenKind.String) // line or named object; followed immediately by an arrow if line
                        {
                            var arrow = third.Remainder.ConsumeToken();
                            if (arrow.HasValue && arrow.Value.Kind is TokenKind.LeftArrow or TokenKind.RightArrow or TokenKind.NoArrow or TokenKind.DoubleArrow)
                            {
                                return Line.Select(x => (AST.UntypedDocumentContent)x)(input);
                            }
                            else
                            {
                                return Object.Select(x => (AST.UntypedDocumentContent)x)(input);
                            }
                        }
                        else if (third.Value.Kind == TokenKind.LeftParenthesis) // line or anonymous object; followed by a call then a target if line
                        {
                            var call = ClassCall(classList.Remainder);
                            if (call.HasValue)
                            {
                                var lineFrom = Shared.Target(call.Remainder);
                                if (lineFrom.HasValue)
                                {
                                    return Line.Select(x => (AST.UntypedDocumentContent)x)(input);
                                }
                                else
                                {
                                    return Object.Select(x => (AST.UntypedDocumentContent)x)(input);
                                }
                            }
                        }
                        else // still a (slightly less trivial) object
                        {
                            return Object.Select(x => (AST.UntypedDocumentContent)x)(input);
                        }
                    }
                }
            }

            return TokenListParserResult.Empty<TokenKind, AST.UntypedDocumentContent>(input, new[] { "attribute", "class", "object", "line" });
        };

        public static TokenListParser<TokenKind, AST.UntypedDocument> UntypedDocument { get; } =
            DocumentContent
                .ManyOptionalDelimited()
                .Select(decs => new AST.UntypedDocument(decs.ToArray()))
                .AtEnd();
    }
}
