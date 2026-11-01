using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.CodeBuilder.CSharp.Results;

/// <summary>
/// Parse operation was cancelled.
/// </summary>
[TypeOption(typeof(CodeBuilderCSharpResultCodes), "ParseCancelled", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ParseCancelledCode : CodeBuilderCSharpResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ParseCancelledCode"/> class.
    /// </summary>
    public ParseCancelledCode()
        : base(10010, "ParseCancelled",
            ResultSeverities.ByName("Error"),
            "Parse operation was cancelled",
            isRetryable: true)
    {
    }
}
