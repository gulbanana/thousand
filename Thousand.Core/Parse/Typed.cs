﻿using Superpower;
using Superpower.Model;
using Superpower.Parsers;
using System;
using System.Linq;
using static Superpower.Parse;

namespace Thousand.Parse
{
    /**********************************************************************
     * The typed AST is syntactically complete, with all macros resolved. *
     **********************************************************************/
    public static class Typed
    {
        public static TokenListParser<TokenKind, AST.ObjectAttribute> ObjectAttribute { get; } =
            AttributeParsers.NodeAttribute.Select(x => (AST.ObjectAttribute)x)
                .Or(AttributeParsers.RegionAttribute.Select(x => (AST.ObjectAttribute)x))
                .Or(AttributeParsers.LineAttribute.Select(x => (AST.ObjectAttribute)x))
                .Or(AttributeParsers.PositionAttribute.Select(x => (AST.ObjectAttribute)x))
                .Or(AttributeParsers.TextAttribute.Select(x => (AST.ObjectAttribute)x));

        public static TokenListParser<TokenKind, AST.SegmentAttribute> SegmentAttribute { get; } =
            AttributeParsers.ArrowAttribute.Select(x => (AST.SegmentAttribute)x)
                .Or(AttributeParsers.LineAttribute.Select(x => (AST.SegmentAttribute)x))
                .Or(AttributeParsers.PositionAttribute.Select(x => (AST.SegmentAttribute)x));

        public static TokenListParser<TokenKind, AST.DiagramAttribute> DiagramAttribute { get; } =
            AttributeParsers.DocumentAttribute.Select(x => (AST.DiagramAttribute)x)
                .Or(AttributeParsers.RegionAttribute.Select(x => (AST.DiagramAttribute)x))
                .Or(AttributeParsers.TextAttribute.Select(x => (AST.DiagramAttribute)x));

        public static TokenListParser<TokenKind, AST.EntityAttribute> EntityAttribute { get; } =
            AttributeParsers.LineAttribute.Select(x => (AST.EntityAttribute)x)
                .Or(AttributeParsers.PositionAttribute.Select(x => (AST.EntityAttribute)x));

        /**********************************************************************
         * Classes, the key unit of abstraction, shared by objects and lines. *
         **********************************************************************/

        public static TokenListParser<TokenKind, AST.TypedClass> ObjectClassBody(Identifier name, Identifier[] bases) =>
            from attrs in Shared.List(ObjectAttribute).OptionalOrDefault(Array.Empty<AST.ObjectAttribute>())
            from children in Shared.Scope(ObjectContent).OptionalOrDefault(Array.Empty<AST.TypedObjectContent>())
            select new AST.ObjectClass(name, bases, attrs, children) as AST.TypedClass;

        public static TokenListParser<TokenKind, AST.TypedClass> LineClassBody(Identifier name, Identifier[] bases) =>
            Shared.List(SegmentAttribute).OptionalOrDefault(Array.Empty<AST.SegmentAttribute>()).Select(attrs => new AST.LineClass(name, bases, attrs) as AST.TypedClass);

        public static TokenListParser<TokenKind, AST.TypedClass> ObjectOrLineClassBody(Identifier name, Identifier[] bases) =>
            Shared.List(EntityAttribute).OptionalOrDefault(Array.Empty<AST.EntityAttribute>()).Select(attrs => new AST.ObjectOrLineClass(name, bases, attrs) as AST.TypedClass);

        public static TokenListParser<TokenKind, AST.TypedClass> ClassBody(Identifier name, Identifier[] bases) => input =>
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
                var lineOnlyAttr = AttributeParsers.ArrowOnlyAttribute(remainder);
                var eitherAttr = EntityAttribute(remainder);
                if (eitherAttr.HasValue && !lineOnlyAttr.HasValue)
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

                var lineAttr = SegmentAttribute(remainder);
                if (lineAttr.HasValue)
                {
                    return LineClassBody(name, bases)(input);
                }

                var objectAttr = ObjectAttribute(remainder);
                if (objectAttr.HasValue)
                {
                    return ObjectClassBody(name, bases)(input);
                }

                return TokenListParserResult.CastEmpty<TokenKind, Unit, AST.TypedClass>(ObjectAttribute.Value(Unit.Value).Or(SegmentAttribute.Value(Unit.Value))(remainder));
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

        public static TokenListParser<TokenKind, AST.TypedObjectContent> ObjectContent { get; } = input =>
        {
            var fail = TokenListParserResult.Empty<TokenKind, AST.TypedObjectContent>(input, new[] { "attribute", "class", "object", "line" });

            var first = input.ConsumeToken();
            if (!first.HasValue)
            {
                return fail;
            }
            else if (first.Value.Kind == TokenKind.ClassKeyword)
            {
                return Class.Select(x => (AST.TypedObjectContent)x)(input);
            }
            else if (first.Value.Kind == TokenKind.Identifier) // could be an attribute, an object or a line
            {
                var second = first.Remainder.ConsumeToken();

                if (!second.HasValue) // this is a trivial object!
                {
                    return Ref(() => Object!).Select(x => (AST.TypedObjectContent)x)(input);
                }
                else if (second.Value.Kind == TokenKind.EqualsSign) // can only be an attribute
                {
                    return ObjectAttribute.Select(x => (AST.TypedObjectContent)x)(input);
                }
                else // could still be an object or a line
                {
                    var classList = Shared.ClassList(input);
                    if (classList.HasValue)
                    {
                        var identifier = classList.Remainder.ConsumeToken(); // object declaration or first object of line
                        if (!identifier.HasValue) // a slightly less trivial object 
                        {
                            return Ref(() => Object!).Select(x => (AST.TypedObjectContent)x)(input);
                        }

                        var arrow = identifier.Remainder.ConsumeToken();
                        if (arrow.HasValue && arrow.Value.Kind is TokenKind.LeftArrow or TokenKind.RightArrow or TokenKind.NoArrow or TokenKind.DoubleArrow)
                        {
                            return Ref(() => Line!).Select(x => (AST.TypedObjectContent)x)(input);
                        }
                        else
                        {
                            return Ref(() => Object!).Select(x => (AST.TypedObjectContent)x)(input);
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

        public static TokenListParser<TokenKind, AST.TypedObject> Object { get; } =
            from classes in Shared.ClassList
            from name in Shared.Target.AsNullable().OptionalOrDefault()
            from attrs in Shared.List(ObjectAttribute).OptionalOrDefault(Array.Empty<AST.ObjectAttribute>())
            from children in Shared.Scope(ObjectContent).OptionalOrDefault(Array.Empty<AST.TypedObjectContent>())
            select new AST.TypedObject(classes, name, attrs, children);

        /*********************************
         * Lines, which connect objects. *
         *********************************/

        public static TokenListParser<TokenKind, AST.TypedLine> Line { get; } =
            from classes in Shared.ClassList
            from chain in Shared.Edges
            from attrs in Shared.List(SegmentAttribute).OptionalOrDefault(Array.Empty<AST.SegmentAttribute>())
            select new AST.TypedLine(classes, chain.ToArray(), attrs);

        /***************************************************************************
         * Entire parsed documents, multiple of which can contribute to a diagram. *
         ***************************************************************************/

        public static TokenListParser<TokenKind, AST.TypedDocumentContent> DocumentContent { get; } = input =>
        {
            var fail = TokenListParserResult.Empty<TokenKind, AST.TypedDocumentContent>(input, new[] { "attribute", "class", "object", "line" });

            var first = input.ConsumeToken();
            if (first.Value.Kind == TokenKind.ClassKeyword) // could be a class declaration
            {
                return Class.Select(x => (AST.TypedDocumentContent)x)(input);
            }
            else if (first.Value.Kind == TokenKind.Identifier) // could be an attribute, an object or a line
            {
                var second = first.Remainder.ConsumeToken();

                if (!second.HasValue) // this is a trivial object!
                {
                    return Object.Select(x => (AST.TypedDocumentContent)x)(input);
                }
                if (second.Value.Kind == TokenKind.EqualsSign) // can only be an attribute
                {
                    return DiagramAttribute.Select(x => (AST.TypedDocumentContent)x)(input);
                }
                else // could still be an object or a line
                {
                    var classList = Shared.ClassList(input);
                    if (classList.HasValue)
                    {
                        var identifier = classList.Remainder.ConsumeToken(); // object declaration or first object of line
                        if (!identifier.HasValue) // a slightly less trivial object 
                        {
                            return Object.Select(x => (AST.TypedDocumentContent)x)(input);
                        }

                        var arrow = identifier.Remainder.ConsumeToken();
                        if (arrow.HasValue && arrow.Value.Kind is TokenKind.LeftArrow or TokenKind.RightArrow or TokenKind.NoArrow or TokenKind.DoubleArrow)
                        {
                            return Line.Select(x => (AST.TypedDocumentContent)x)(input);
                        }
                        else
                        {
                            return Object.Select(x => (AST.TypedDocumentContent)x)(input);
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
            DocumentContent
                .ManyOptionalDelimited()
                .Select(decs => new AST.TypedDocument(decs.ToArray()))
                .AtEnd();
    }
}
