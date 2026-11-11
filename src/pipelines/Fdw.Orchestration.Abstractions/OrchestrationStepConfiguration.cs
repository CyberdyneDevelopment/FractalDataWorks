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

    /// <summary>Gets or sets the step's name.</summary>
    public string Name { get; set; } = string.Empty;


    /// <summary>Gets or sets the service type this record configures: "OrchestrationStep" here, a derived kind's own name in a derived record.</summary>
    public virtual string ServiceType { get; set; } = "OrchestrationStep";

    /// <summary>Gets or sets the maximum number of retry attempts for this step.</summary>
    public int MaxRetries { get; set; }

    /// <summary>Gets or sets the delay between retry attempts.</summary>
    public TimeSpan RetryDelay { get; set; }
}
