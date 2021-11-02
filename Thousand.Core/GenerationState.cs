using Superpower.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using Thousand.Model;

namespace Thousand
{
    public class GenerationState
    {
        private List<GenerationError> warnings { get; } = new();
        private List<GenerationError> errors { get; } = new();
        private Dictionary<string, TextSpan> sourceMap { get; } = new();

        public int ErrorCount()
        {
            return errors.Count;
        }

        public bool HasErrors()
        {
            return errors.Any();
        }

        public bool HasWarnings()
        {
            return warnings.Any();
        }

        public GenerationError[] GetErrors()
        {
            return Map(errors).ToArray();
        }

        public GenerationError[] GetWarnings()
        {
            return Map(warnings).ToArray();
        }

        public string JoinErrors()
        {
            var mappedErrors = GetErrors();
            return string.Join(Environment.NewLine, mappedErrors.Select(e => e.ToString()));
        }

        public string JoinWarnings()
        {
            var mappedWarnings = GetWarnings();
            return string.Join(Environment.NewLine, mappedWarnings.Select(e => e.ToString()));
        }

        public void MapSpan(string uniqueString, TextSpan span)
        {
            sourceMap[uniqueString] = span;
        }

        public void AddError(Exception e)
        {
            errors.Add(new(TextSpan.Empty, e));
        }

        public void AddError(TextSpan span, ErrorKind kind, string message, params Name[] identifiers)
        {
            var formatted = string.Format(message.Replace("`{`", "`{{`").Replace("`}`", "`}}`"), identifiers.Select(i => "`" + i.AsMap(sourceMap) + "`").ToArray());
            errors.Add(new(span, kind, formatted));
        }

        public void AddErrorEx(TextSpan span, ErrorKind kind, string message, params (Name name, string suffix)[] identifiers)
        {
            var formatted = string.Format(message.Replace("`{`", "`{{`").Replace("`}`", "`}}`"), identifiers.Select(i => "`" + i.name.AsMap(sourceMap) + i.suffix + "`").ToArray());
            errors.Add(new(span, kind, formatted));
        }

        public void AddError(Name source, ErrorKind kind, string message, params Name[] identifiers)
        {
            AddError(source.AsLoc, kind, message, identifiers);
        }

        public void AddErrorEx(Name source, ErrorKind kind, string message, params (Name, string)[] identifiers)
        {
            AddErrorEx(source.AsLoc, kind, message, identifiers);
        }

        public void AddError<T>(TokenList<Parse.TokenKind> tokens, TextSpan endSpan, TokenListParserResult<Parse.TokenKind, T> error)
        {
            var badSpan = error.Location.IsAtEnd ? 
                tokens.IsAtEnd ? endSpan :
                tokens.Last().Span : error.Location.First().Span;
            AddError(badSpan, ErrorKind.Syntax, error.FormatErrorMessageFragment());
        }

        public void AddWarning(Exception e)
        {
            warnings.Add(new(TextSpan.Empty, e));
        }

        public void AddWarning(TextSpan span, ErrorKind kind, string message, params Name[] identifiers)
        {
            var formatted = string.Format(message, identifiers.Select(i => "`" + i.AsMap(sourceMap) + "`").ToArray());
            warnings.Add(new(span, kind, formatted));
        }

        public void AddWarning(Name source, ErrorKind kind, string message, params Name[] identifiers)
        {
            AddWarning(source.AsLoc, kind, message, identifiers);
        }

        private IEnumerable<GenerationError> Map(IEnumerable<GenerationError> errors)
        {
            return errors.Select(e => new GenerationError(
                sourceMap.ContainsKey(e.Span.ToStringValue()) ? sourceMap[e.Span.ToStringValue()] : e.Span,
                e.Kind,
                e.Message,
                e.Details
            ));
        }

        internal TextSpan UnmapSpan(TextSpan unsource) => sourceMap.ContainsKey(unsource.ToStringValue()) ? sourceMap[unsource.ToStringValue()] : unsource;
    }
}
