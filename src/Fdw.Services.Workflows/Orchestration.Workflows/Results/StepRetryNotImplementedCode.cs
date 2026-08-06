using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Orchestration.Workflows.Results;

/// <summary>
/// Step retry not yet implemented for in-memory orchestrator.
/// </summary>
[TypeOption(typeof(OrchestratedWorkflowResultCodes), "StepRetryNotImplemented", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class StepRetryNotImplementedCode : WorkflowResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StepRetryNotImplementedCode"/> class.
    /// </summary>
    public StepRetryNotImplementedCode()
        : base(90005, "StepRetryNotImplemented",
            ResultSeverities.ByName("Error"),
            "Step retry not yet implemented for in-memory orchestrator",
            isRetryable: false)
    {
    }
}
