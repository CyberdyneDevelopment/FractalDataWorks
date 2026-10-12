using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Scheduling.Abstractions;

/// <summary>
/// Defines the contract for tasks that can be scheduled and executed within the Fdw framework.
/// </summary>
public interface IScheduledTask
{
    /// <summary>
    /// Gets the unique identifier for this task.
    /// </summary>
    string TaskId { get; }

    /// <summary>
    /// Gets the name of this task.
    /// </summary>
    string TaskName { get; }

    /// <summary>
    /// Gets the category of this task.
    /// </summary>
    string TaskCategory { get; }

    /// <summary>
    /// Gets the priority of this task.
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// Gets the expected execution time for this task.
    /// </summary>
    TimeSpan? ExpectedExecutionTime { get; }

    /// <summary>
    /// Gets the maximum allowed execution time for this task.
    /// </summary>
    TimeSpan? MaxExecutionTime { get; }

    /// <summary>
    /// Gets the list of task dependencies.
    /// </summary>
    IReadOnlyList<string> Dependencies { get; }

    /// <summary>
    /// Gets the task configuration.
    /// </summary>
    IReadOnlyDictionary<string, object> Configuration { get; }

    /// <summary>
    /// Gets the task metadata.
    /// </summary>
    IReadOnlyDictionary<string, object> Metadata { get; }

    /// <summary>
    /// Validates that the task can be executed with the current configuration.
    /// </summary>
    /// <returns>A result indicating whether the task is valid and can be executed.</returns>
    IGenericResult Validate();

    /// <summary>
    /// Executes the task within the provided execution context.
    /// </summary>
    /// <param name="context">The task execution context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the execution operation.</returns>
    Task<IGenericResult> Execute(ITaskExecutionContext context, CancellationToken cancellationToken = default);
}