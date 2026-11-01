using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.CodeBuilder.CSharp.Results;

/// <summary>
/// Parse operation failed with exception.
/// </summary>
[TypeOption(typeof(CodeBuilderCSharpResultCodes), "ParseFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ParseFailedCode : CodeBuilderCSharpResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ParseFailedCode"/> class.
    /// </summary>
    public ParseFailedCode()
        : base(90003, "ParseFailed",
            ResultSeverities.ByName("Error"),
            "Parse error: {ErrorMessage}",
            isRetryable: false)
    {
    }
}
