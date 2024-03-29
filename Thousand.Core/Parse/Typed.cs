﻿using Superpower;
using Superpower.Model;
using Superpower.Parsers;
using System;
using System.Collections.Generic;
using System.Linq;
using Thousand.API;
using Thousand.Model;
using static Superpower.Parse;

namespace Thousand.Parse
{
    /*************************************************************************************
     * The typed AST is syntactically complete, with macros resolved and errors removed. *
     * This is a reference parser used for testing and error message generation.         *
     * In production, the untyped AST is typechecked into a valid AST without reparsing. *
     *************************************************************************************/
    public static class Typed
    {
        public static TokenListParser<TokenKind, TA[]> AttributeList<TA>(TokenListParser<TokenKind, TA> attributeParser) =>
            from begin in Token.EqualTo(TokenKind.LeftBracket)
            from values in attributeParser.ManyDelimitedBy(Token.EqualTo(TokenKind.Comma))
            from end in Token.EqualTo(TokenKind.RightBracket)
            select values;

        public static TokenListParser<TokenKind, IReadOnlyList<T>> DeclarationScope<T>(TokenListParser<TokenKind, T> pT) =>
            from begin in Token.EqualTo(TokenKind.LeftBrace)
            from decs in pT.ManyOptionalDelimitedBy(TokenKind.LineSeparator, TokenKind.RightBrace)
            from end in Token.EqualTo(TokenKind.RightBrace)
            select decs;

        /************************************************************
         * Base attribute groups, delegated to metadata definitions *
         ************************************************************/
        public static TokenListParser<TokenKind, AST.ArrowAttribute> ArrowAttribute { get; } = Attribute.From(ArrowAttributes.All());
        public static TokenListParser<TokenKind, AST.EntityAttribute> EntityAttribute { get; } = Attribute.From(EntityAttributes.All());
        public static TokenListParser<TokenKind, AST.NodeAttribute> NodeAttribute { get; } = Attribute.From(NodeAttributes.All());
        public static TokenListParser<TokenKind, AST.RegionAttribute> RegionAttribute { get; } = Attribute.From(RegionAttributes.All());
        public static TokenListParser<TokenKind, AST.TextAttribute> TextAttribute { get; } = Attribute.From(TextAttributes.All());        

        /******************************************************************************
         * Attribute group combinations which apply to each class of document entity. *
         ******************************************************************************/
        public static TokenListParser<TokenKind, AST.DocumentAttribute> DocumentAttribute { get; } =
            RegionAttribute.Select(x => (AST.DocumentAttribute)x)
                .Or(TextAttribute.Select(x => (AST.DocumentAttribute)x));

        public static TokenListParser<TokenKind, AST.ObjectAttribute> ObjectAttribute { get; } =
            NodeAttribute.Select(x => (AST.ObjectAttribute)x)
                .Or(RegionAttribute.Select(x => (AST.ObjectAttribute)x))
                .Or(EntityAttribute.Select(x => (AST.ObjectAttribute)x))
                .Or(TextAttribute.Select(x => (AST.ObjectAttribute)x));

        public static TokenListParser<TokenKind, AST.LineAttribute> LineAttribute { get; } =
            ArrowAttribute.Select(x => (AST.LineAttribute)x)
                .Or(EntityAttribute.Select(x => (AST.LineAttribute)x));

        /**********************************************************************
         * Classes, the key unit of abstraction, shared by objects and lines. *
         **********************************************************************/

        public static TokenListParser<TokenKind, AST.TypedClass> ObjectClassBody(Name name, Name[] bases) =>
            from attrs in AttributeList(ObjectAttribute).OptionalOrDefault(Array.Empty<AST.ObjectAttribute>())
            from children in DeclarationScope(Ref(() => Declaration!)).OptionalOrDefault(Array.Empty<AST.TypedDeclaration>())
            select new AST.ObjectClass(name, bases, attrs, children) as AST.TypedClass;

        public static TokenListParser<TokenKind, AST.TypedClass> LineClassBody(Name name, Name[] bases) =>
            AttributeList(LineAttribute).OptionalOrDefault(Array.Empty<AST.LineAttribute>()).Select(attrs => new AST.LineClass(name, bases, attrs) as AST.TypedClass);

        public static TokenListParser<TokenKind, AST.TypedClass> ObjectOrLineClassBody(Name name, Name[] bases) =>
            AttributeList(EntityAttribute).OptionalOrDefault(Array.Empty<AST.EntityAttribute>()).Select(attrs => new AST.ObjectOrLineClass(name, bases, attrs) as AST.TypedClass);

        public static TokenListParser<TokenKind, AST.TypedClass> ClassBody(Name name, Name[] bases) => input =>
        {
            var beginAttrs = Token.EqualTo(TokenKind.LeftBracket)(input);
            if (!beginAttrs.HasValue)
            {
                if (Token.EqualTo(TokenKind.LeftBrace)(input).HasValue)
                {
                    return ObjectClassBody(name, bases)(input);
                }
                else
                {
                    return ObjectOrLineClassBody(name, bases)(input);
                }
            }

            var remainder = beginAttrs.Remainder;
            while (true)
            {                
                var eitherAttr = EntityAttribute(remainder);
                if (eitherAttr.HasValue)
                {
                    if (eitherAttr.Value.IsLineOnly())
                    {
                        return LineClassBody(name, bases)(input);
                    }
                    else
                    {
                        remainder = eitherAttr.Remainder;
                        var next = remainder.ConsumeToken();

                        if (!next.HasValue)
                        {
                            return ObjectOrLineClassBody(name, bases)(input);
                        }
                        else if (next.Value.Kind == TokenKind.Comma)
                        {
                            remainder = next.Remainder;
                            continue;
                        }
                        else if (next.Value.Kind == TokenKind.RightBracket)
                        {
                            remainder = next.Remainder;
                            break;
                        }
                    }
                }

                var lineAttr = LineAttribute(remainder);
                if (lineAttr.HasValue)
                {
                    return LineClassBody(name, bases)(input);
                }

                var objectAttr = ObjectAttribute(remainder);
                if (objectAttr.HasValue)
                {
                    return ObjectClassBody(name, bases)(input);
                }

                return TokenListParserResult.CastEmpty<TokenKind, Unit, AST.TypedClass>(ObjectAttribute.Value(Unit.Value).Or(LineAttribute.Value(Unit.Value))(remainder));
            }

            if (remainder.ConsumeToken() is { HasValue: true, Value: { Kind: TokenKind.LeftBrace } })
            {
                return ObjectClassBody(name, bases)(input);
            }
            else
            {
                return ObjectOrLineClassBody(name, bases)(input);
            }
        };

        public static TokenListParser<TokenKind, AST.TypedClass> Class { get; } =
            from keyword in Token.EqualTo(TokenKind.ClassKeyword)
            from name in Identifier.Any
            from bases in Shared.BaseClasses
            from klass in ClassBody(name, bases)
            select klass;

        /********************************************************************
         * Objects, containing intrinsic drawable content and more objects. *
         ********************************************************************/

        public static TokenListParser<TokenKind, AST.TypedObject> Object { get; } =
            from classes in Shared.ClassList
            from name in Shared.ObjectReference.AsNullable().OptionalOrDefault()
            from attrs in AttributeList(ObjectAttribute).OptionalOrDefault(Array.Empty<AST.ObjectAttribute>())
            from children in DeclarationScope(Ref(() => Declaration!)).OptionalOrDefault(Array.Empty<AST.TypedDeclaration>())
            select new AST.TypedObject(classes, name, attrs, children);

        /*********************************
         * Lines, which connect objects. *
         *********************************/

        public static TokenListParser<TokenKind, Func<IMacro<bool>, AST.TypedObject>> ObjectFactory { get; } =
            Object.Select<TokenKind, AST.TypedObject, Func<IMacro<bool>, AST.TypedObject>>(o => _ => o);

        public static TokenListParser<TokenKind, AST.TypedLine> Line { get; } =
            from classes in Shared.ClassList
            from content in Shared.LineSegments(ObjectFactory)
            from attrs in AttributeList(LineAttribute).OptionalOrDefault(Array.Empty<AST.LineAttribute>())
            select new AST.TypedLine(classes, content.ToArray(), attrs);

        /***************************************************************************
         * Entire parsed documents, multiple of which can contribute to a diagram. *
         ***************************************************************************/

        public static TokenListParser<TokenKind, AST.TypedDeclaration> Declaration { get; } = input =>
        {
            var fail = TokenListParserResult.Empty<TokenKind, AST.TypedDeclaration>(input, new[] { "attribute", "class", "object", "line" });

            var first = input.ConsumeToken();
            if (!first.HasValue)
            {
                return fail;
            }
            else if (first.Value.Kind == TokenKind.ClassKeyword)
            {
                return Class.Select(x => (AST.TypedDeclaration)x)(input);
            }
            else if (first.Value.Kind == TokenKind.Identifier) // could be an attribute, an object or a line
            {
                var second = first.Remainder.ConsumeToken();

                if (!second.HasValue) // this is a trivial object!
                {
                    return Ref(() => Object!).Select(x => (AST.TypedDeclaration)x)(input);
                }
                else if (second.Value.Kind == TokenKind.LeftParenthesisUnbound)
                {
                    return Ref(() => Line!).Select(x => (AST.TypedDeclaration)x)(input);
                }
                else // could still be an object or a line
                {
                    var classList = Shared.ClassList(input);
                    if (classList.HasValue)
                    {
                        var identifierOrInline = classList.Remainder.ConsumeToken(); // object declaration or first object of line
                        if (!identifierOrInline.HasValue) // a slightly less trivial object 
                        {
                            return Ref(() => Object!).Select(x => (AST.TypedDeclaration)x)(input);
                        }
                        else if (identifierOrInline.Value.Kind == TokenKind.LeftParenthesisUnbound) // a line which begins with an inline object
                        {
                            return Ref(() => Line!).Select(x => (AST.TypedDeclaration)x)(input);
                        }

                        var arrow = identifierOrInline.Remainder.ConsumeToken();
                        if (arrow.HasValue && arrow.Value.Kind is TokenKind.Arrow)
                        {
                            return Ref(() => Line!).Select(x => (AST.TypedDeclaration)x)(input);
                        }
                        else
                        {
                            return Ref(() => Object!).Select(x => (AST.TypedDeclaration)x)(input);
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

        public static TokenListParser<TokenKind, AST.TypedDocument> Document { get; } =
            Declaration
                .ManyOptionalDelimitedBy(TokenKind.LineSeparator)
                .Select(decs => new AST.TypedDocument(decs))
                .AtEnd();
    }
}
