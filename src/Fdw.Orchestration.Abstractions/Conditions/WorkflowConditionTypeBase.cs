using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;

namespace Fdw.Orchestration.Workflows.Abstractions.WorkflowConditionTypeOptions;

/// <summary>
/// Base class for workflow condition types.
/// </summary>
/// <ExcludeFromCoverageReason>TypeOption base class - no logic to test</ExcludeFromCoverageReason>
[ExcludeFromCodeCoverage]
public abstract class WorkflowConditionTypeBase : TypeOptionBase<int, WorkflowConditionTypeBase>, IWorkflowConditionType
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WorkflowConditionTypeBase"/> class.
    /// </summary>
    /// <param name="id">The unique identifier for this workflow condition type.</param>
    /// <param name="name">The name of this workflow condition type.</param>
    /// <param name="alwaysTrue">Whether this condition always evaluates to true.</param>
    /// <param name="requiresExpression">Whether this condition requires expression evaluation.</param>
    protected WorkflowConditionTypeBase(int id, string name, bool alwaysTrue, bool requiresExpression)
        : base(id, name)
    {
        AlwaysTrue = alwaysTrue;
        RequiresExpression = requiresExpression;
    }

    /// <inheritdoc />
    public bool AlwaysTrue { get; }

    /// <inheritdoc />
    public bool RequiresExpression { get; }
}
