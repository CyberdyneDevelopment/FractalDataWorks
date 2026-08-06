using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.CodeBuilder.CSharp.Results;

/// <summary>
/// Source code contains syntax errors.
/// </summary>
[TypeOption(typeof(CodeBuilderCSharpResultCodes), "SyntaxErrors", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class SyntaxErrorsCode : CodeBuilderCSharpResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SyntaxErrorsCode"/> class.
    /// </summary>
    public SyntaxErrorsCode()
        : base(20001, "SyntaxErrors",
            ResultSeverities.ByName("Error"),
            "Source code contains {ErrorCount} syntax error(s)",
            isRetryable: false)
    {
    }
}
