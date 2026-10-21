using System;
using Fdw.Configuration;

namespace Fdw.Services.Pipelines.Abstractions;

/// <summary>A webhook trigger (FDW-1884): the address an outside system calls, with a secret, to start one pipeline.</summary>
public interface IWebhookTriggerImplementationConfiguration : IImplementationConfiguration
{
    /// <summary>Gets or sets the durable identity of the pipeline the trigger starts.</summary>
    Guid PipelineId { get; set; }

    /// <summary>Gets or sets the name of that pipeline when the trigger was saved; it is shown, never used to start a run.</summary>
    string PipelineName { get; set; }

    /// <summary>Gets or sets whether the trigger is on. A call to a trigger that is off is answered as a call to an unknown one.</summary>
    bool IsEnabled { get; set; }

    /// <summary>Gets or sets what the trigger is for.</summary>
    string? Description { get; set; }

    /// <summary>Gets or sets the SHA-256 of the trigger's secret as lowercase hexadecimal; the secret itself is never stored.</summary>
    string SecretHash { get; set; }

    /// <summary>Gets or sets the first characters of the secret, for telling secrets apart.</summary>
    string SecretPrefix { get; set; }

    /// <summary>Gets or sets when the current secret was made.</summary>
    DateTimeOffset SecretCreatedAt { get; set; }

    /// <summary>Gets or sets the tenant the trigger belongs to, which its runs act in.</summary>
    Guid? TenantId { get; set; }
}
