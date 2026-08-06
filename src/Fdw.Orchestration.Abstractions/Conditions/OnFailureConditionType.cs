using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Orchestration.Workflows.Abstractions.WorkflowConditionTypeOptions;

/// <summary>
/// Run if previous step failed.
/// </summary>
/// <ExcludeFromCoverageReason>TypeOption - no logic to test</ExcludeFromCoverageReason>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(WorkflowConditionTypes), "OnFailure", RestrictToCurrentCompilation = true)]
public sealed class OnFailureConditionType : WorkflowConditionTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OnFailureConditionType"/> class.
    /// </summary>
    public OnFailureConditionType() : base(2, "OnFailure", alwaysTrue: false, requiresExpression: false) { }
}
