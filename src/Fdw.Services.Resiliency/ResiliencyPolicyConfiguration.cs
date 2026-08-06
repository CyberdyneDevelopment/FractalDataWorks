using System;
using Fdw.Data;

namespace Fdw.Services.Resiliency;

/// <summary>
/// Maps to <c>settings.ResiliencyPolicy</c> — user-defined resiliency policies.
/// </summary>
[GenerateMapper]
public sealed partial class ResiliencyPolicyConfiguration
{

    /// <summary>Gets or sets the durable logical identity.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the policy name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the maximum number of retry attempts.</summary>
    public int MaxRetries { get; set; }

    /// <summary>Gets or sets the initial retry delay in milliseconds.</summary>
    public int RetryDelayMs { get; set; }

    /// <summary>Gets or sets the backoff multiplier for exponential retry.</summary>
    public decimal BackoffMultiplier { get; set; }

    /// <summary>Gets or sets the maximum delay in milliseconds between retries.</summary>
    public int MaxDelayMs { get; set; }

    /// <summary>Gets or sets the optional operation timeout in milliseconds.</summary>
    public int? TimeoutMs { get; set; }

    /// <summary>Gets or sets the optional circuit breaker failure threshold.</summary>
    public int? CircuitBreakerThreshold { get; set; }

    /// <summary>Gets or sets the optional circuit breaker open duration in milliseconds.</summary>
    public int? CircuitBreakerDurationMs { get; set; }

    /// <summary>Gets or sets whether this is the current active version.</summary>
    public bool IsCurrent { get; set; } = true;

    /// <summary>Gets or sets whether this record has been soft-deleted.</summary>
    public bool IsDeleted { get; set; }

    /// <summary>Gets or sets the original creation date from the source system.</summary>
    public DateTimeOffset? SrcCreateDate { get; set; }

    /// <summary>Gets or sets the timestamp when the record was created.</summary>
    public DateTimeOffset CreateDate { get; set; }

    /// <summary>Gets or sets the database user who created the record.</summary>
    public string CreateBy { get; set; } = string.Empty;

    /// <summary>Gets or sets the application user on whose behalf the record was created.</summary>
    public string CreateOnBehalfOf { get; set; } = string.Empty;

    /// <summary>Gets or sets the timestamp when the record was last modified.</summary>
    public DateTimeOffset ModifyDate { get; set; }

    /// <summary>Gets or sets the database user who last modified the record.</summary>
    public string ModifyBy { get; set; } = string.Empty;

    /// <summary>Gets or sets the application user on whose behalf the record was last modified.</summary>
    public string ModifyOnBehalfOf { get; set; } = string.Empty;
}
