using System;
using Fdw.Orchestration.Abstractions.TypeCollections.ErrorHandlingModeOptions;

namespace Fdw.Orchestration.Abstractions;

/// <summary>
/// Default implementation of <see cref="IExecutionPolicyContext"/>.
/// Immutable after construction; populated via object initializer.
/// </summary>
public sealed class ExecutionPolicyContext : IExecutionPolicyContext
{
    /// <inheritdoc/>
    public IErrorHandlingMode? DefaultErrorHandling { get; init; }

    /// <inheritdoc/>
    public bool ContinueOnFailure { get; init; }

    /// <inheritdoc/>
    public TimeSpan? ResultCacheDuration { get; init; }

    /// <summary>
    /// Gets a policy context with no overrides — caller-supplied defaults take over.
    /// </summary>
    public static ExecutionPolicyContext Default { get; } = new();
}
