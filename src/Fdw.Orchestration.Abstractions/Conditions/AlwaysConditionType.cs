using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Orchestration.Workflows.Abstractions.WorkflowConditionTypeOptions;

/// <summary>
/// Always run (no condition).
/// </summary>
/// <ExcludeFromCoverageReason>TypeOption - no logic to test</ExcludeFromCoverageReason>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(WorkflowConditionTypes), "Always", RestrictToCurrentCompilation = true)]
public sealed class AlwaysConditionType : WorkflowConditionTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AlwaysConditionType"/> class.
    /// </summary>
    public AlwaysConditionType() : base(0, "Always", alwaysTrue: true, requiresExpression: false) { }
}
