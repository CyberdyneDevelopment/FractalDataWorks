using System;
using Fdw.Configuration;

namespace Fdw.Orchestration.Abstractions;

/// <summary>
/// Base configuration for an orchestration step (retry policy, identity).
/// </summary>
public class OrchestrationStepConfiguration : IGenericConfiguration
{
    /// <inheritdoc/>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <inheritdoc/>
    public string Name { get; set; } = string.Empty;

    /// <inheritdoc/>
    public virtual string SectionName { get; set; } = "OrchestrationSteps";

    /// <inheritdoc/>
    public virtual string ServiceType { get; set; } = "OrchestrationStep";

    /// <inheritdoc/>
    public string? ServiceOptionType { get; set; }

    /// <summary>Gets or sets the maximum number of retry attempts for this step.</summary>
    public int MaxRetries { get; set; }

    /// <summary>Gets or sets the delay between retry attempts.</summary>
    public TimeSpan RetryDelay { get; set; }
}
