using Superpower.Model;
using System;
using System.Collections.Generic;
using System.Linq;

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
            errors.Add(new(e));
        }

        public void AddError(TextSpan span, ErrorKind kind, string message, params Parse.Identifier[] identifiers)
        {
            var formatted = string.Format(message.Replace("`{`", "`{{`").Replace("`}`", "`}}`"), identifiers.Select(i => "`" + i.DisplayName(sourceMap) + "`").ToArray());
            errors.Add(new(span, kind, formatted));
        }

        public void AddError(Parse.Identifier source, ErrorKind kind, string message, params Parse.Identifier[] identifiers)
        {
            AddError(source.Span, kind, message, identifiers);
        }

        public void AddWarning(Exception e)
        {
            warnings.Add(new(e));
        }

        public void AddWarning(TextSpan span, ErrorKind kind, string message, params Parse.Identifier[] identifiers)
        {
            var formatted = string.Format(message, identifiers.Select(i => "`" + i.DisplayName(sourceMap) + "`").ToArray());
            warnings.Add(new(span, kind, formatted));
        }

        public void AddWarning(Parse.Identifier source, ErrorKind kind, string message, params Parse.Identifier[] identifiers)
        {
            AddWarning(source.Span, kind, message, identifiers);
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
    }
}
