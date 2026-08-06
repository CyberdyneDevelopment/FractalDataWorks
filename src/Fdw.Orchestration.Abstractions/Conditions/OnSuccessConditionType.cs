using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Orchestration.Workflows.Abstractions.WorkflowConditionTypeOptions;

/// <summary>
/// Run if previous step succeeded.
/// </summary>
/// <ExcludeFromCoverageReason>TypeOption - no logic to test</ExcludeFromCoverageReason>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(WorkflowConditionTypes), "OnSuccess", RestrictToCurrentCompilation = true)]
public sealed class OnSuccessConditionType : WorkflowConditionTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OnSuccessConditionType"/> class.
    /// </summary>
    public OnSuccessConditionType() : base(1, "OnSuccess", alwaysTrue: false, requiresExpression: false) { }
}
