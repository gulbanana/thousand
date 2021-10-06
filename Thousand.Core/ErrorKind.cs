namespace Thousand
{
    public enum ErrorKind
    {
        /// <summary>Unexpected implementation bugs</summary>
        Internal,

        /// <summary>Tokenize/parse errors, many of which are effectively type errors</summary>
        Syntax,

        /// <summary>Semantic errors relating to names</summary>
        Reference,

        /// <summary>Semantic errors relating to classes</summary>
        Type,

        /// <summary>Constraint-solving errors</summary>
        Layout
    }
}
