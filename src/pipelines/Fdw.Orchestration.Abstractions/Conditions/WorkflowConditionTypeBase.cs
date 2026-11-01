using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;

namespace Fdw.Orchestration.Workflows.Abstractions.WorkflowConditionTypeOptions;

/// <summary>
/// Base class for workflow condition types.
/// </summary>
/// <ExcludeFromCoverageReason>TypeOption base class - no logic to test</ExcludeFromCoverageReason>
[ExcludeFromCodeCoverage]
public abstract class WorkflowConditionTypeBase : TypeOptionBase<WorkflowConditionTypeBase>, IWorkflowConditionType
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WorkflowConditionTypeBase"/> class.
    /// </summary>
    /// <param name="name">The name of this workflow condition type.</param>
    /// <param name="alwaysTrue">Whether this condition always evaluates to true.</param>
    /// <param name="requiresExpression">Whether this condition requires expression evaluation.</param>
    protected WorkflowConditionTypeBase(string name, bool alwaysTrue, bool requiresExpression)
        : base(name)
    {
        AlwaysTrue = alwaysTrue;
        RequiresExpression = requiresExpression;
    }

    /// <inheritdoc />
    public bool AlwaysTrue { get; }

    /// <inheritdoc />
    public bool RequiresExpression { get; }
}
