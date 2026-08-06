using System.Collections.Generic;

namespace Fdw.Orchestration.Workflows.Abstractions;

/// <summary>
/// Represents a group of steps that can execute in parallel.
/// </summary>
public interface IExecutionGroup
{
    /// <summary>
    /// Gets the execution level (0 = first to execute, higher = later).
    /// </summary>
    int Level { get; }

    /// <summary>
    /// Gets the steps in this execution group.
    /// </summary>
    IReadOnlyList<IWorkflowStep> Steps { get; }

    /// <summary>
    /// Gets whether steps in this group can execute in parallel.
    /// </summary>
    bool CanExecuteInParallel { get; }
}