using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.CodeBuilder.CSharp.Results;

/// <summary>
/// Validation failed.
/// </summary>
[TypeOption(typeof(CodeBuilderCSharpResultCodes), "ValidationFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ValidationFailedCode : CodeBuilderCSharpResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationFailedCode"/> class.
    /// </summary>
    public ValidationFailedCode()
        : base(21000, "ValidationFailed",
            ResultSeverities.ByName("Error"),
            "Validation failed",
            isRetryable: false)
    {
    }
}
