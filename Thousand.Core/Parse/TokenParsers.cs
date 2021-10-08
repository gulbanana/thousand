using Superpower;
using Superpower.Model;
using Superpower.Parsers;
using System;
using System.Collections.Generic;
using System.Linq;
using Thousand.Model;

namespace Thousand.Parse
{
    public static class TokenParsers
    {
        public static TokenListParser<TokenKind, Unit> NewLine { get; } =
            Token.EqualTo(TokenKind.LineSeparator).Value(Unit.Value);

        public static TokenListParser<TokenKind, Identifier> Target { get; } =
            Value.StringIdentifier.Or(Identifier.Any);

        public static TokenListParser<TokenKind, TA[]> AttributeList<TA>(TokenListParser<TokenKind, TA> attributeParser) =>
            from begin in Token.EqualTo(TokenKind.LeftBracket)
            from values in attributeParser.AtLeastOnceDelimitedBy(Token.EqualTo(TokenKind.Comma))
            from end in Token.EqualTo(TokenKind.RightBracket)
            select values;

        public static TokenListParser<TokenKind, Identifier[]> ClassList { get; } =
            Identifier.Any.AtLeastOnceDelimitedBy(Token.EqualTo(TokenKind.Period));

        public static TokenListParser<TokenKind, Macro[]> CallArgs { get; } =
            from begin in Token.EqualTo(TokenKind.LeftParenthesis)
            from arguments in Value.Macro(TokenKind.Comma, TokenKind.RightParenthesis).AtLeastOnceDelimitedBy(Token.EqualTo(TokenKind.Comma))
            from end in Token.EqualTo(TokenKind.RightParenthesis)
            select arguments;

        public static TokenListParser<TokenKind, AST.ClassCall> ClassCall { get; } =
            from name in Identifier.Any
            from arguments in CallArgs.OptionalOrDefault(Array.Empty<Macro>())
            select new AST.ClassCall(name, arguments);

        public static TokenListParser<TokenKind, AST.ClassCall[]> ClassCallList { get; } =
            ClassCall.Templated().AtLeastOnceDelimitedBy(Token.EqualTo(TokenKind.Period));

        public static TokenListParser<TokenKind, Identifier[]> BaseClasses { get; } =
            Token.EqualTo(TokenKind.Colon)
                 .IgnoreThen(ClassList)
                 .OptionalOrDefault(Array.Empty<Identifier>());

        public static TokenListParser<TokenKind, AST.UntypedAttribute> UntypedAttribute { get; } =
            from key in Identifier.Any.Named("attribute name")
            from _ in Token.EqualTo(TokenKind.EqualsSign)
            from value in Value.Macro(TokenKind.Comma, TokenKind.RightBracket)
            select new AST.UntypedAttribute(key, value);

        public static TokenListParser<TokenKind, AST.ObjectAttribute> ObjectAttribute { get; } =
            AttributeParsers.NodeAttribute.Select(x => (AST.ObjectAttribute)x)
                .Or(AttributeParsers.RegionAttribute.Select(x => (AST.ObjectAttribute)x))
                .Or(AttributeParsers.StrokeAttribute.Select(x => (AST.ObjectAttribute)x))
                .Or(AttributeParsers.TextAttribute.Select(x => (AST.ObjectAttribute)x));

        public static TokenListParser<TokenKind, AST.UntypedObject> UntypedObject { get; } =
            from classes in ClassCallList
            from name in Target.AsNullable().OptionalOrDefault()
            from attrs in AttributeList(ObjectAttribute).OptionalOrDefault(Array.Empty<AST.ObjectAttribute>())
            from children in Superpower.Parse.Ref(() => Scope!).OptionalOrDefault(Array.Empty<AST.ObjectDeclaration>())
            select new AST.UntypedObject(classes, name, attrs, children);

        public static TokenListParser<TokenKind, AST.TypedObject> TypedObject { get; } =
            from classes in ClassList
            from name in Target.AsNullable().OptionalOrDefault()
            from attrs in AttributeList(ObjectAttribute).OptionalOrDefault(Array.Empty<AST.ObjectAttribute>())
            from children in Superpower.Parse.Ref(() => Scope!).OptionalOrDefault(Array.Empty<AST.ObjectDeclaration>())
            select new AST.TypedObject(classes, name, attrs, children);

        public static TokenListParser<TokenKind, IEnumerable<AST.LineSegment>> TerminalEdge { get; } =
            from dst in Target
            select Enumerable.Repeat(new AST.LineSegment(dst, null), 1);

        public static TokenListParser<TokenKind, ArrowKind> Arrow { get; } =
            Token.EqualTo(TokenKind.RightArrow).Value(ArrowKind.Forward)
                .Or(Token.EqualTo(TokenKind.LeftArrow).Value(ArrowKind.Backward))
                .Or(Token.EqualTo(TokenKind.NoArrow).Value(ArrowKind.Neither))
                .Or(Token.EqualTo(TokenKind.DoubleArrow).Value(ArrowKind.Both));

        public static TokenListParser<TokenKind, IEnumerable<AST.LineSegment>> Edges { get; } =
            from src in Target
            from arrow in Arrow
            from next in Superpower.Parse.Ref(() => Edges!).Try().Or(TerminalEdge)
            select next.Prepend(new(src, arrow));

        public static TokenListParser<TokenKind, AST.SegmentAttribute> LineAttribute { get; } =
            AttributeParsers.ArrowAttribute.Select(x => (AST.SegmentAttribute)x)
                .Or(AttributeParsers.StrokeAttribute.Select(x => (AST.SegmentAttribute)x));

        public static TokenListParser<TokenKind, AST.UntypedLine> UntypedLine { get; } =
            from calls in ClassCallList
            from chain in Edges
            from attrs in AttributeList(LineAttribute).OptionalOrDefault(Array.Empty<AST.SegmentAttribute>())
            select new AST.UntypedLine(calls, chain.ToArray(), attrs);

        public static TokenListParser<TokenKind, AST.TypedLine> TypedLine { get; } =
            from classes in ClassList
            from chain in Edges
            from attrs in AttributeList(LineAttribute).OptionalOrDefault(Array.Empty<AST.SegmentAttribute>())
            select new AST.TypedLine(classes, chain.ToArray(), attrs);

        public static TokenListParser<TokenKind, AST.DiagramAttribute> DiagramAttribute { get; } =
            AttributeParsers.DocumentAttribute.Select(x => (AST.DiagramAttribute)x)
                .Or(AttributeParsers.RegionAttribute.Select(x => (AST.DiagramAttribute)x))
                .Or(AttributeParsers.TextAttribute.Select(x => (AST.DiagramAttribute)x));

        public static TokenListParser<TokenKind, AST.ArgumentList> ClassArgs { get; } =
            from begin in Token.EqualTo(TokenKind.LeftParenthesis)
            from arguments in Identifier.Variable.AtLeastOnceDelimitedBy(Token.EqualTo(TokenKind.Comma))
            from end in Token.EqualTo(TokenKind.RightParenthesis)
            select new AST.ArgumentList(arguments);

        public static TokenListParser<TokenKind, AST.UntypedClass> UntypedClass { get; } =
            from keyword in Token.EqualTo(TokenKind.ClassKeyword)
            from name in Identifier.Any
            from arguments in ClassArgs.Templated()
            from bases in BaseClasses
            from attrs in AttributeList(UntypedAttribute).OptionalOrDefault(Array.Empty<AST.UntypedAttribute>())
            select new AST.UntypedClass(name, arguments, bases, attrs);

        public static TokenListParser<TokenKind, AST.TypedClass> ObjectClassBody(Identifier name, Identifier[] bases) =>
            AttributeList(ObjectAttribute).OptionalOrDefault(Array.Empty<AST.ObjectAttribute>()).Select(attrs => new AST.ObjectClass(name, bases, attrs) as AST.TypedClass);

        public static TokenListParser<TokenKind, AST.TypedClass> LineClassBody(Identifier name, Identifier[] bases) =>
            AttributeList(LineAttribute).OptionalOrDefault(Array.Empty<AST.SegmentAttribute>()).Select(attrs => new AST.LineClass(name, bases, attrs) as AST.TypedClass);

        public static TokenListParser<TokenKind, AST.TypedClass> ObjectOrLineClassBody(Identifier name, Identifier[] bases) =>
            AttributeList(AttributeParsers.StrokeAttribute).OptionalOrDefault(Array.Empty<AST.LineAttribute>()).Select(attrs => new AST.ObjectOrLineClass(name, bases, attrs) as AST.TypedClass);

        public static TokenListParser<TokenKind, AST.TypedClass> TypedClassBody(Identifier name, Identifier[] bases) => input =>
        {
            var begin = Token.EqualTo(TokenKind.LeftBracket)(input);
            if (!begin.HasValue)
            {
                // XXX process object-class scopes
                return TokenListParserResult.Value<TokenKind, AST.TypedClass>(new AST.ObjectOrLineClass(name, bases, Array.Empty<AST.LineAttribute>()), input, input);
            }

            var remainder = begin.Remainder;
            while (true)
            {
                var eitherAttr = AttributeParsers.StrokeAttribute(remainder);
                if (eitherAttr.HasValue)
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
                        break;
                    }
                }

                var objectAttr = ObjectAttribute(remainder);
                if (objectAttr.HasValue)
                {
                    return ObjectClassBody(name, bases)(input);
                }

                var lineAttr = LineAttribute(remainder);
                if (lineAttr.HasValue)
                {
                    return LineClassBody(name, bases)(input);
                }

                return TokenListParserResult.CastEmpty<TokenKind, Unit, AST.TypedClass>(ObjectAttribute.Value(Unit.Value).Or(LineAttribute.Value(Unit.Value))(remainder));
            }

            // XXX process object-class scopes
            return ObjectOrLineClassBody(name, bases)(input);
        };

        public static TokenListParser<TokenKind, AST.TypedClass> TypedClass { get; } =
            from keyword in Token.EqualTo(TokenKind.ClassKeyword)
            from name in Identifier.Any
            from bases in BaseClasses
            from klass in TypedClassBody(name, bases)
            select klass;

        public static TokenListParser<TokenKind, AST.ObjectDeclaration> ObjectDeclaration { get; } = input =>
        {
            var fail = TokenListParserResult.Empty<TokenKind, AST.ObjectDeclaration>(input, new[] { "attribute", "object", "line" });

            var first = input.ConsumeToken();
            if (first.Value.Kind == TokenKind.Identifier) // could be an attribute, an object or a line
            {
                var second = first.Remainder.ConsumeToken();
                
                if (!second.HasValue) // this is a trivial object!
                {
                    return TypedObject.Select(x => (AST.ObjectDeclaration)x)(input);
                }
                else if (second.Value.Kind == TokenKind.EqualsSign) // can only be an attribute
                {
                    return ObjectAttribute.Select(x => (AST.ObjectDeclaration)x)(input);
                }
                else // could still be an object or a line
                {
                    var classList = ClassList(input);
                    if (classList.HasValue)
                    {
                        var identifier = classList.Remainder.ConsumeToken(); // object declaration or first object of line
                        if (!identifier.HasValue) // a slightly less trivial object 
                        {
                            return TypedObject.Select(x => (AST.ObjectDeclaration)x)(input);
                        }

                        var arrow = identifier.Remainder.ConsumeToken();
                        if (arrow.HasValue && arrow.Value.Kind is TokenKind.LeftArrow or TokenKind.RightArrow or TokenKind.NoArrow or TokenKind.DoubleArrow)
                        {
                            return TypedLine.Select(x => (AST.ObjectDeclaration)x)(input);
                        }
                        else
                        {
                            return TypedObject.Select(x => (AST.ObjectDeclaration)x)(input);
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

        public static TokenListParser<TokenKind, AST.ObjectDeclaration[]> Scope { get; } =
            from begin in Token.EqualTo(TokenKind.LeftBrace)
            from decs in ObjectDeclaration.ManyOptionalDelimited(terminator: TokenKind.RightBrace)
            from end in Token.EqualTo(TokenKind.RightBrace)
            select decs.ToArray();

        // handwritten top-level parser, because the language syntax is pretty ambiguous. if parsed with combinators, the errors aren't very good
        public static TokenListParser<TokenKind, AST.UntypedDocumentDeclaration> UntypedDocumentDeclaration { get; } = input =>
        {
            var fail = TokenListParserResult.Empty<TokenKind, AST.UntypedDocumentDeclaration>(input, new[] { "attribute", "class", "object", "line" });

            var first = input.ConsumeToken();
            if (first.Value.Kind == TokenKind.ClassKeyword) // could be a class declaration
            {
                var untypedClass = UntypedClass.Templated().Select(x => (AST.UntypedDocumentDeclaration)x)(input);
                if (untypedClass.HasValue)
                {
                    return untypedClass;
                }
                else
                {
                    return TypedClass.Select(x => (AST.UntypedDocumentDeclaration)x)(input);
                }
            }
            else if (first.Value.Kind == TokenKind.Identifier) // could be an attribute, an object or a line
            {
                var second = first.Remainder.ConsumeToken();

                if (!second.HasValue) // this is a trivial object!
                {
                    return UntypedObject.Select(x => (AST.UntypedDocumentDeclaration)x)(input);
                }
                if (second.Value.Kind == TokenKind.EqualsSign) // can only be an attribute
                {
                    return DiagramAttribute.Select(x => (AST.UntypedDocumentDeclaration)x)(input);
                }
                else // could still be an object or a line
                {
                    var classList = ClassCallList(input);
                    if (classList.HasValue)
                    {
                        var identifier = classList.Remainder.ConsumeToken(); // object declaration or first object of line
                        if (!identifier.HasValue) // a slightly less trivial object 
                        {
                            return UntypedObject.Select(x => (AST.UntypedDocumentDeclaration)x)(input);
                        }

                        var arrow = identifier.Remainder.ConsumeToken();
                        if (arrow.HasValue && arrow.Value.Kind is TokenKind.LeftArrow or TokenKind.RightArrow or TokenKind.NoArrow or TokenKind.DoubleArrow)
                        {
                            return UntypedLine.Select(x => (AST.UntypedDocumentDeclaration)x)(input);
                        }
                        else
                        {
                            return UntypedObject.Select(x => (AST.UntypedDocumentDeclaration)x)(input);
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

        public static TokenListParser<TokenKind, AST.TypedDocumentDeclaration> TypedDocumentDeclaration { get; } = input =>
        {
            var fail = TokenListParserResult.Empty<TokenKind, AST.TypedDocumentDeclaration>(input, new[] { "attribute", "class", "object", "line" });
            
            var first = input.ConsumeToken();
            if (first.Value.Kind == TokenKind.ClassKeyword) // could be a class declaration
            {
                return TypedClass.Select(x => (AST.TypedDocumentDeclaration)x)(input);
            }
            else if (first.Value.Kind == TokenKind.Identifier) // could be an attribute, an object or a line
            {
                var second = first.Remainder.ConsumeToken();

                if (!second.HasValue) // this is a trivial object!
                {
                    return TypedObject.Select(x => (AST.TypedDocumentDeclaration)x)(input);
                }
                if (second.Value.Kind == TokenKind.EqualsSign) // can only be an attribute
                {
                    return DiagramAttribute.Select(x => (AST.TypedDocumentDeclaration)x)(input);
                }
                else // could still be an object or a line
                {
                    var classList = ClassList(input);
                    if (classList.HasValue)
                    {
                        var identifier = classList.Remainder.ConsumeToken(); // object declaration or first object of line
                        if (!identifier.HasValue) // a slightly less trivial object 
                        {
                            return TypedObject.Select(x => (AST.TypedDocumentDeclaration)x)(input);
                        }

                        var arrow = identifier.Remainder.ConsumeToken();
                        if (arrow.HasValue && arrow.Value.Kind is TokenKind.LeftArrow or TokenKind.RightArrow or TokenKind.NoArrow or TokenKind.DoubleArrow)
                        {
                            return TypedLine.Select(x => (AST.TypedDocumentDeclaration)x)(input);
                        }
                        else
                        {
                            return TypedObject.Select(x => (AST.TypedDocumentDeclaration)x)(input);
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

        public static TokenListParser<TokenKind, AST.UntypedDocument> UntypedDocument { get; } =
            UntypedDocumentDeclaration
                .ManyOptionalDelimited()
                .Select(decs => new AST.UntypedDocument(decs.ToArray()))
                .AtEnd();

        public static TokenListParser<TokenKind, AST.TypedDocument> TypedDocument { get; } =
            TypedDocumentDeclaration
                .ManyOptionalDelimited()
                .Select(decs => new AST.TypedDocument(decs.ToArray()))
                .AtEnd();
    }
}
