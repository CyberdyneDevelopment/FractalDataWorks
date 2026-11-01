using Fdw.Collections;

namespace Fdw.Orchestration.Workflows.Abstractions.WorkflowConditionTypeOptions;

/// <summary>
/// Interface for workflow condition types.
/// Extends ITypeOption to enable TypeCollection discovery.
/// </summary>
public interface IWorkflowConditionType : ITypeOption<int, WorkflowConditionTypeBase>
{
    /// <summary>
    /// Gets a value indicating whether this condition always evaluates to true.
    /// </summary>
    bool AlwaysTrue { get; }

    /// <summary>
    /// Gets a value indicating whether this condition requires expression evaluation.
    /// </summary>
    bool RequiresExpression { get; }
}
