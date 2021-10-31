using Superpower;
using Superpower.Model;
using Superpower.Parsers;
using System;
using System.Collections.Generic;
using System.Linq;
using static Superpower.Parse;

namespace Thousand.Parse
{
    /****************************************************************************
     * The untyped AST is error-tolerant, supporting the presentation compiler. *
     * It must be preprocessed as well as type-checked before evaluation.       *
     ****************************************************************************/
    public static class Untyped
    {
        /**************************************************
         * Class calls - invocations of template classes. *
         **************************************************/

        public static TokenListParser<TokenKind, IMacro[]> CallArgs { get; } =
            from begin in Token.EqualTo(TokenKind.LeftParenthesisBound)
            from arguments in Macro.Raw(TokenKind.Comma, TokenKind.RightParenthesis).AtLeastOnceDelimitedBy(Token.EqualTo(TokenKind.Comma))
            from end in Token.EqualTo(TokenKind.RightParenthesis)
            select arguments;

        public static TokenListParser<TokenKind, AST.ClassCall> ClassCall { get; } =
            from name in Identifier.Any
            from arguments in CallArgs.OptionalOrDefault(Array.Empty<IMacro>())
            select new AST.ClassCall(name, arguments);

        public static TokenListParser<TokenKind, IMacro<AST.ClassCall?>[]> ClassCallList { get; } =
            Macro.Of(ClassCall.AsNullable())
                .Or(Macro.Empty(default(AST.ClassCall?)))
                .AtLeastOnceDelimitedBy(Token.EqualTo(TokenKind.Period));

        /******************************************************************************************************
         * Attributes in the untyped AST are a key-value pair with the value's tokenlist preserved for        *
         * further parsing. The list can contain empty positions, which are used as a marker for completions. *
         ******************************************************************************************************/
        public static TokenListParser<TokenKind, IMacro> AttributeValue { get; } =
            Macro.Raw(TokenKind.Comma, TokenKind.LineSeparator, TokenKind.RightBracket, TokenKind.RightBrace, TokenKind.LineSeparator);

        public static TokenListParser<TokenKind, AST.UntypedAttribute> Attribute { get; } =
            from key in Identifier.Any.Named("attribute name")
            from value in Token.EqualTo(TokenKind.EqualsSign)
                               .IgnoreThen(AttributeValue.Select(m => (eq: true, m)))
                               .Or(AttributeValue.Select(m => (eq: false, m)))
            select new AST.UntypedAttribute(key, value.eq, value.m);

        public static TokenListParser<TokenKind, AST.Attributes> AttributeList { get; } =
            from begin in Token.EqualTo(TokenKind.LeftBracket)
            from values in Attribute.ManyOptionalDelimitedBy(TokenKind.Comma, TokenKind.RightBracket, empty: (input, remainder) => new AST.UntypedAttribute(null, false, new Macro(input, remainder)))
            from end in Macro.Of(Token.EqualTo(TokenKind.RightBracket).Value(true).OptionalOrDefault(false))
            select new AST.Attributes(end, values);

        /*****************************************************************************************
         * Declaration scopes can also contain empty positions, or they can be /invalid/,        *
         * meaning that they don't parse as but /may/ do so if macro-expansion is run. Some      *
         * invalid declarations may be actual errors, which will be reparsed by the typechecker. *
         *****************************************************************************************/

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

        public static TokenListParser<TokenKind, IMacro<AST.UntypedDeclaration>> Declaration { get; } = input =>
        {
            var fail = TokenListParserResult.Empty<TokenKind, IMacro<AST.UntypedDeclaration>>(input, new[] { "attribute", "class", "object", "line" });
            var c = Macro.Of(Ref(() => Class!)).Select(x => (IMacro<AST.UntypedDeclaration>)x);
            var o = Macro.Of(Ref(() => Object!)).Select(x => (IMacro<AST.UntypedDeclaration>)x);
            var l = Macro.Of(Ref(() => Line!)).Select(x => (IMacro<AST.UntypedDeclaration>)x);

            var first = input.ConsumeToken();
            if (!first.HasValue)
            {
                return fail;
            }
            else if (first.Value.Kind == TokenKind.ClassKeyword)
            {
                return c(input);
            }
            else if (first.Value.Kind == TokenKind.Identifier) // an entity (object or line)
            {
                var second = first.Remainder.ConsumeToken();

                if (!second.HasValue) // this is a trivial object!
                {
                    return o(input);
                }
                else if (second.Value.Kind == TokenKind.LeftParenthesisUnbound) // line with an inline
                {
                    return l(input);
                }
                else // could still be an object or a line
                {
                    var classList = ClassCallList(input);
                    if (classList.HasValue)
                    {
                        var identifier = classList.Remainder.ConsumeToken(); // object declaration or first object of line
                        if (!identifier.HasValue) // a slightly less trivial object 
                        {
                            return o(input);
                        }
                        else if (identifier.Value.Kind == TokenKind.LeftParenthesisUnbound) // a line which begins with an inline object
                        {
                            return l(input);
                        }

                        var arrow = identifier.Remainder.ConsumeToken();
                        if (arrow.HasValue && arrow.Value.Kind == TokenKind.Arrow)
                        {
                            return l(input);
                        }
                        else
                        {
                            return o(input);
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

        public static TokenListParser<TokenKind, IReadOnlyList<IMacro<AST.UntypedDeclaration>>> DeclarationScope { get; } =
            from begin in Token.EqualTo(TokenKind.LeftBrace)
            from decs in Declaration.ManyOptionalDelimitedBy(
                TokenKind.LineSeparator, 
                TokenKind.RightBrace, 
                empty: (input, remainder) => new Macro<AST.EmptyDeclaration>(input, remainder, new AST.EmptyDeclaration()),
                invalid: (input, remainder) => new Macro<AST.InvalidDeclaration>(input, remainder, new AST.InvalidDeclaration()))
            from end in Token.EqualTo(TokenKind.RightBrace)
            select decs;

        /**************************************************************
         * Classes, which may be templates requiring macro-expansion. *
         **************************************************************/

        public static TokenListParser<TokenKind, AST.Argument> ClassArg { get; } =
            from name in Identifier.Variable
            from @default in Token.EqualTo(TokenKind.EqualsSign).IgnoreThen(Macro.Raw(TokenKind.Comma, TokenKind.RightParenthesis)).AsNullable().OptionalOrDefault()
            select new AST.Argument(name, @default);

        public static TokenListParser<TokenKind, AST.Argument[]> ClassArgs { get; } =
            from begin in Token.EqualTo(TokenKind.LeftParenthesisBound)
            from arguments in ClassArg.AtLeastOnceDelimitedBy(Token.EqualTo(TokenKind.Comma))
            from end in Token.EqualTo(TokenKind.RightParenthesis)
            select arguments;

        public static TokenListParser<TokenKind, AST.UntypedClass> Class { get; } =
            from keyword in Token.EqualTo(TokenKind.ClassKeyword)
            from name in Identifier.Any
            from arguments in Macro.Of(ClassArgs.OptionalOrDefault(Array.Empty<AST.Argument>()))
            from bases in Token.EqualTo(TokenKind.Colon).IgnoreThen(ClassCallList).OptionalOrDefault(Array.Empty<Macro<AST.ClassCall?>>())
            from attrs in AttributeList.Or(Macro.Empty(true).Select(m => new AST.Attributes(m, Array.Empty<AST.UntypedAttribute>())))
            from children in DeclarationScope.OptionalOrDefault(Array.Empty<Macro<AST.UntypedDeclaration>>())
            select new AST.UntypedClass(name, arguments, bases, attrs, children);

        /*************************************************************************************
         * Objects and lines, which can instantiate template classes with a macro expansion. *
         *************************************************************************************/

        public static TokenListParser<TokenKind, AST.UntypedObject> Object { get; } =
            from classes in ClassCallList
            from name in Shared.ObjectReference.AsNullable().OptionalOrDefault()
            from attrs in AttributeList.Or(Macro.Empty(true).Select(m => new AST.Attributes(m, Array.Empty<AST.UntypedAttribute>())))
            from children in DeclarationScope.OptionalOrDefault(Array.Empty<Macro<AST.UntypedDeclaration>>())
            select new AST.UntypedObject(classes, name, attrs, children);

        public static TokenListParser<TokenKind, Func<IMacro<bool>, AST.UntypedInline>> Inline { get; } =
            Macro.Of(Object).Select<TokenKind, IMacro<AST.UntypedObject>, Func<IMacro<bool>, AST.UntypedInline>>(o => b => new AST.UntypedInline(b, o));

        public static TokenListParser<TokenKind, AST.UntypedLine> Line { get; } =
            from calls in ClassCallList
            from chain in Shared.LineSegments(Inline)
            from attrs in AttributeList.Or(Macro.Empty(true).Select(m => new AST.Attributes(m, Array.Empty<AST.UntypedAttribute>())))
            select new AST.UntypedLine(calls, chain.ToArray(), attrs);

        /*********************************************************************************************
         * Documents which require macro resolution, containing both valid and invalid declarations. *
         *********************************************************************************************/

        public static TokenListParser<TokenKind, AST.UntypedDocument> Document { get; } =
            Declaration
                 .ManyOptionalDelimitedBy(
                     TokenKind.LineSeparator,
                     invalid: (input, remainder) => new Macro<AST.InvalidDeclaration>(input, remainder, new AST.InvalidDeclaration()),
                     empty: (input, remainder) => new Macro<AST.EmptyDeclaration>(input, remainder, new AST.EmptyDeclaration()))
                 .Select(decs => new AST.UntypedDocument(decs))
                 .AtEnd();
    }
}
