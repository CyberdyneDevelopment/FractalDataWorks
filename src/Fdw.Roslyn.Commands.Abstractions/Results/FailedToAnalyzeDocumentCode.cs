using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Roslyn.Commands.Abstractions.Results;

/// <summary>
/// Failed to analyze document.
/// </summary>
[TypeOption(typeof(RoslynResultCodes), "FailedToAnalyzeDocument", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class FailedToAnalyzeDocumentCode : RoslynResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FailedToAnalyzeDocumentCode"/> class.
    /// </summary>
    public FailedToAnalyzeDocumentCode()
        : base(91003, "FailedToAnalyzeDocument",
            ResultSeverities.ByName("Error"),
            "Failed to analyze document",
            isRetryable: false)
    {
    }
}
