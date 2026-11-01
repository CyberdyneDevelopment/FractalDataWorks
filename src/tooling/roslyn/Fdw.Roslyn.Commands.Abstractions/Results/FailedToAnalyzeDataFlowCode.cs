using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Roslyn.Commands.Abstractions.Results;

/// <summary>
/// Failed to analyze data flow.
/// </summary>
[TypeOption(typeof(RoslynResultCodes), "FailedToAnalyzeDataFlow", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class FailedToAnalyzeDataFlowCode : RoslynResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FailedToAnalyzeDataFlowCode"/> class.
    /// </summary>
    public FailedToAnalyzeDataFlowCode()
        : base(91002, "FailedToAnalyzeDataFlow",
            ResultSeverities.ByName("Error"),
            "Failed to analyze data flow",
            isRetryable: false)
    {
    }
}
