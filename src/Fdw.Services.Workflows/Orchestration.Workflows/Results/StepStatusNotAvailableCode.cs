using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Orchestration.Workflows.Results;

/// <summary>
/// Step status not available for completed local execution.
/// </summary>
[TypeOption(typeof(OrchestratedWorkflowResultCodes), "StepStatusNotAvailable", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class StepStatusNotAvailableCode : WorkflowResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StepStatusNotAvailableCode"/> class.
    /// </summary>
    public StepStatusNotAvailableCode()
        : base(40000, "StepStatusNotAvailable",
            ResultSeverities.ByName("Warning"),
            "Step status not available for completed local execution",
            isRetryable: false)
    {
    }
}
