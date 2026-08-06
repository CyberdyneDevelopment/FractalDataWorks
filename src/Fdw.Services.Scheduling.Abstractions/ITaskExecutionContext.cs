using System;
using Fdw.Orchestration.Abstractions;

namespace Fdw.Services.Scheduling.Abstractions;

/// <summary>
/// Provides context information and services for task execution.
/// </summary>
/// <remarks>
/// Extends <see cref="IExecutionContext"/> with scheduling-task-specific state:
/// typed property bag, maximum execution time, and strongly-typed property access.
/// Universal per-run fields (ExecutionId, CancellationToken, Logger, Services,
/// StartTime, Parameters, SharedState) are inherited from <see cref="IExecutionContext"/>.
/// </remarks>
// Why: Previously redeclared ExecutionId/CancellationToken/Logger/ServiceProvider/StartTime
// independently. All universal fields are now inherited. ServiceProvider is exposed as
// IExecutionContext.Services to maintain consistency across the execution context hierarchy.
public interface ITaskExecutionContext : IExecutionContext
{
    /// <summary>
    /// Gets the maximum execution time allowed for the task.
    /// </summary>
    TimeSpan? MaxExecutionTime { get; }

    /// <summary>
    /// Gets or sets custom properties for the execution context.
    /// </summary>
    System.Collections.Generic.IReadOnlyDictionary<string, object> Properties { get; }

    /// <summary>
    /// Sets a custom property for the execution context.
    /// </summary>
    /// <param name="key">The property key.</param>
    /// <param name="value">The property value.</param>
    void SetProperty(string key, object value);

    /// <summary>
    /// Gets a custom property value by key.
    /// </summary>
    /// <typeparam name="T">The type of the property value.</typeparam>
    /// <param name="key">The property key.</param>
    /// <returns>The property value, or default if not found.</returns>
    T? GetProperty<T>(string key);
}
