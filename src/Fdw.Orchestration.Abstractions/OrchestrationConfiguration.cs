using System;
using Fdw.Configuration;
using Fdw.Orchestration.Abstractions.TypeCollections.ErrorHandlingModeOptions;

namespace Fdw.Orchestration.Abstractions;

/// <summary>
/// Base configuration for orchestrations.
/// Holds identity + cross-cutting policies (error handling, caching, timeout).
/// </summary>
public class OrchestrationConfiguration : IGenericConfiguration
{
    /// <inheritdoc/>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <inheritdoc/>
    public string Name { get; set; } = string.Empty;

    /// <inheritdoc/>
    public virtual string SectionName { get; set; } = "Orchestrations";

    /// <inheritdoc/>
    public virtual string ServiceType { get; set; } = "Orchestration";

    /// <inheritdoc/>
    public string? ServiceOptionType { get; set; }

    /// <summary>Gets or sets the maximum execution time for the entire orchestration.</summary>
    public TimeSpan? ExecutionTimeout { get; set; }

    /// <summary>Gets or sets the default error handling mode for steps that don't specify their own.</summary>
    public IErrorHandlingMode? DefaultErrorHandling { get; set; }

    /// <summary>Gets or sets a value indicating whether to continue execution if a step fails and error handling allows it.</summary>
    public bool ContinueOnStepFailure { get; set; }

    /// <summary>Gets or sets a value indicating whether step results should be cached.</summary>
    public bool EnableResultCaching { get; set; }

    /// <summary>Gets or sets the cache duration for step results.</summary>
    public TimeSpan? ResultCacheDuration { get; set; }
}
