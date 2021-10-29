﻿using Superpower;
using Superpower.Model;
using Superpower.Parsers;
using System;
using System.Linq;
using static Superpower.Parse;

namespace Thousand.Parse
{   
    public static class Untyped
    {
        public static TokenListParser<TokenKind, Macro> AttributeValue { get; } =
            Macro.Raw(TokenKind.Comma, TokenKind.LineSeparator, TokenKind.RightBracket, TokenKind.RightBrace, TokenKind.LineSeparator);

        public static TokenListParser<TokenKind, AST.UntypedAttribute> Attribute { get; } =
            from key in Identifier.Any.Named("attribute name")
            from value in Token.EqualTo(TokenKind.EqualsSign)
                               .IgnoreThen(AttributeValue.Select(m => (eq: true, m)))
                               .Or(AttributeValue.Select(m => (eq: false, m)))
            select new AST.UntypedAttribute(key, value.eq, value.m);

        public static TokenListParser<TokenKind, AST.UntypedAttribute[]> AttributeList { get; } =
            from begin in Token.EqualTo(TokenKind.LeftBracket)
            from values in Attribute.Or(Macro.Empty().Select(finalValue => new AST.UntypedAttribute(new Identifier("") { Span = finalValue.Span() }, false, finalValue))).ManyDelimitedBy(Token.EqualTo(TokenKind.Comma))
            from end in Token.EqualTo(TokenKind.RightBracket)
            select values;

        /***********************************************************************************************
         * The untyped AST may contain errors and macros. A declaration in any scope can be /invalid/, *
         * meaning that it doesn't parse as but /may/ do so if macro-expansion is run. Some invalid    *
         * declarations may be actual errors, which will not be allowed in the typed AST.              *
         ***********************************************************************************************/

        public static TokenListParser<TokenKind, AST.InvalidDeclaration> InvalidDeclaration { get; } = input =>
        {
            var remainder = input;
            while (!remainder.IsAtEnd)
            {
                var next = remainder.ConsumeToken();                
                if (!next.HasValue || next.Value.Kind == TokenKind.LineSeparator || next.Value.Kind == TokenKind.RightBrace)
                {
                    break;
                }
                remainder = next.Remainder;
            }

            return TokenListParserResult.Value(new AST.InvalidDeclaration(), input, remainder);
        };

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

        public static TokenListParser<TokenKind, Macro<AST.ClassCall?>[]> ClassCallList { get; } =
            Macro.Of(ClassCall.AsNullable())
                .Or(Macro.Empty(default(AST.ClassCall?)))
                .AtLeastOnceDelimitedBy(Token.EqualTo(TokenKind.Period));

        /**************************************************************
         * Classes, which may be templates requiring macro-expansion. *
         **************************************************************/

        public static TokenListParser<TokenKind, AST.Argument> ClassArg { get; } =
            from name in Identifier.Variable
            from @default in Token.EqualTo(TokenKind.EqualsSign).IgnoreThen(Macro.Raw(TokenKind.Comma, TokenKind.RightParenthesis)).AsNullable().OptionalOrDefault()
            select new AST.Argument(name, @default);

        public static TokenListParser<TokenKind, AST.Argument[]> ClassArgs { get; } =
            from begin in Token.EqualTo(TokenKind.LeftParenthesis)
            from arguments in ClassArg.AtLeastOnceDelimitedBy(Token.EqualTo(TokenKind.Comma))
            from end in Token.EqualTo(TokenKind.RightParenthesis)
            select arguments;

        public static TokenListParser<TokenKind, AST.UntypedClass> Class { get; } =
            from keyword in Token.EqualTo(TokenKind.ClassKeyword)
            from name in Identifier.Any
            from arguments in Macro.Of(ClassArgs.OptionalOrDefault(Array.Empty<AST.Argument>()))
            from bases in Token.EqualTo(TokenKind.Colon).IgnoreThen(ClassCallList).OptionalOrDefault(Array.Empty<Macro<AST.ClassCall?>>())
            from attrs in AttributeList.OptionalOrDefault(Array.Empty<AST.UntypedAttribute>())
            let declaration = Ref(() => ObjectContent!).Try().Or(InvalidDeclaration.Select(x => (AST.UntypedObjectContent)x))
            from children in Shared.Scope(
                Macro.Of(declaration),
                invalid: (input, remainder) => new Macro<AST.UntypedObjectContent>(input, remainder, new AST.InvalidDeclaration()),
                fallback: (input, remainder) => new Macro<AST.UntypedObjectContent>(input, remainder, new AST.EmptyDeclaration())
            ).OptionalOrDefault(Array.Empty<Macro<AST.UntypedObjectContent>>())
            select new AST.UntypedClass(name, arguments, bases, attrs, children);

        /*************************************************************************************
         * Objects and lines, which can instantiate template classes with a macro expansion. *
         *************************************************************************************/

        public static TokenListParser<TokenKind, AST.UntypedObjectContent> ObjectContent { get; } = input =>
        {
            var fail = TokenListParserResult.Empty<TokenKind, AST.UntypedObjectContent>(input, new[] { "attribute", "class", "object", "line" });

            var first = input.ConsumeToken();
            if (!first.HasValue)
            {
                return InvalidDeclaration.Select(x => (AST.UntypedObjectContent)x)(input);
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
                    return Attribute.Select(x => (AST.UntypedObjectContent)x)(input);
                }
                else if (second.Value.Kind == TokenKind.Pipe)
                {
                    return Ref(() => Line!).Select(x => (AST.UntypedObjectContent)x)(input);
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
                        else if (identifier.Value.Kind == TokenKind.Pipe) // a line which begins with an inline object
                        {
                            return Ref(() => Line!).Select(x => (AST.UntypedObjectContent)x)(input);
                        }

                        var arrow = identifier.Remainder.ConsumeToken();
                        if (arrow.HasValue && arrow.Value.Kind is TokenKind.LeftArrow or TokenKind.RightArrow or TokenKind.NoArrow or TokenKind.DoubleArrow)
                        {
                            return Ref(() => Line!).Select(x => (AST.UntypedObjectContent)x)(input);
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
            from name in Shared.ObjectReference.AsNullable().OptionalOrDefault()
            from attrs in AttributeList.OptionalOrDefault(Array.Empty<AST.UntypedAttribute>())
            let declaration = ObjectContent.Try().Or(InvalidDeclaration.Select(x => (AST.UntypedObjectContent)x))
            from children in Shared.Scope(
                Macro.Of(declaration),
                invalid: (input, remainder) => new Macro<AST.UntypedObjectContent>(input, remainder, new AST.InvalidDeclaration()),
                fallback: (input, remainder) => new Macro<AST.UntypedObjectContent>(input, remainder, new AST.EmptyDeclaration())
            ).OptionalOrDefault(Array.Empty<Macro<AST.UntypedObjectContent>>())
            select new AST.UntypedObject(classes, name, attrs, children);

        public static TokenListParser<TokenKind, AST.UntypedLine> Line { get; } =
            from calls in ClassCallList
            from chain in Shared.LineSegments(Macro.Of(Object))
            from attrs in AttributeList.OptionalOrDefault(Array.Empty<AST.UntypedAttribute>())
            select new AST.UntypedLine(calls, chain.ToArray(), attrs);

        /*********************************************************************************************
         * Documents which require macro resolution, containing both valid and invalid declarations. *
         *********************************************************************************************/

        public static TokenListParser<TokenKind, AST.UntypedDocumentContent> DocumentContent { get; } = input =>
        {
            var fail = TokenListParserResult.Empty<TokenKind, AST.UntypedDocumentContent>(input, new[] { "attribute", "class", "object", "line" });

            var first = input.ConsumeToken();
            if (first.Value.Kind == TokenKind.ClassKeyword) // could be a class declaration
            {
                return Class.Select(x => (AST.UntypedDocumentContent)x)(input);
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
                    return Attribute.Select(x => (AST.UntypedDocumentContent)x)(input);
                }
                else if (second.Value.Kind == TokenKind.Pipe)
                {
                    return Line.Select(x => (AST.UntypedDocumentContent)x)(input);
                }
                else // could still be an object or a line
                {
                    var classList = ClassCallList(input);
                    if (classList.HasValue)
                    {
                        var identifierOrInline = classList.Remainder.ConsumeToken(); // object declaration or first object of line
                        if (!identifierOrInline.HasValue) // a slightly less trivial object 
                        {
                            return Object.Select(x => (AST.UntypedDocumentContent)x)(input);
                        }
                        else if (identifierOrInline.Value.Kind == TokenKind.Pipe) // a line which begins with an inline object
                        {
                            return Line.Select(x => (AST.UntypedDocumentContent)x)(input);
                        }

                        var arrow = identifierOrInline.Remainder.ConsumeToken();
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
            Macro.Of(DocumentContent.Try().Or(InvalidDeclaration.Select(x => (AST.UntypedDocumentContent)x)))
                .ManyOptionalDelimited(
                    invalid: (input, remainder) => new Macro<AST.UntypedDocumentContent>(input, remainder, new AST.InvalidDeclaration()),
                    fallback: (input, remainder) => new Macro<AST.UntypedDocumentContent>(input, remainder, new AST.EmptyDeclaration()))
                .Select(decs => new AST.UntypedDocument(decs.ToArray()))
                .AtEnd();
    }
}
