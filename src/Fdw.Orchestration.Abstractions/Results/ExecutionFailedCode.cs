using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Orchestration.Abstractions.Results;

/// <summary>
/// Orchestration execution failed.
/// </summary>
[TypeOption(typeof(OrchestrationResultCodes), "ExecutionFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ExecutionFailedCode : OrchestrationResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExecutionFailedCode"/> class.
    /// </summary>
    public ExecutionFailedCode()
        : base(70001, "ExecutionFailed",
            ResultSeverities.ByName("Error"),
            "Orchestration execution failed: {ErrorMessage}",
            isRetryable: false)
    {
    }
}