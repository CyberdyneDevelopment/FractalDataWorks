using Fdw.Orchestration.Workflows.Abstractions.WorkflowConditionTypeOptions;

namespace Fdw.Orchestration.Workflows.Abstractions;

/// <summary>
/// Represents a condition for workflow execution.
/// </summary>
public interface IWorkflowCondition
{
    /// <summary>
    /// Gets the condition type.
    /// </summary>
    IWorkflowConditionType Type { get; }

    /// <summary>
    /// Gets the expression to evaluate.
    /// </summary>
    string Expression { get; }

    /// <summary>
    /// Evaluates the condition.
    /// </summary>
    /// <param name="context">The workflow execution context.</param>
    /// <returns>True if condition is met, false otherwise.</returns>
    bool Evaluate(IWorkflowExecutionContext context);
}