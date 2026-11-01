using System;
using System.Collections.Generic;

namespace Fdw.Services.Pipelines.Clients.Abstractions;

/// <summary>A webhook trigger as the API writes and reads it. It never carries the secret or its hash.</summary>
public class WebhookPayload
{
    /// <summary>Gets or sets the trigger's name, which is the last part of its address (<c>/etl/webhooks/{name}</c>).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the name of the pipeline the trigger starts.</summary>
    public string Pipeline { get; set; } = string.Empty;

    /// <summary>Gets or sets whether the trigger is on. A request states it; there is no default.</summary>
    public bool? Enabled { get; set; }

    /// <summary>Gets or sets what the trigger is for.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the names of the roles a run started through the trigger acts with.</summary>
    public IList<string> RunRoles { get; set; } = [];

    /// <summary>Gets or sets the mappings from the call's body to the pipeline's parameters.</summary>
    public IList<WebhookParameterMapPayload> Parameters { get; set; } = [];

    /// <summary>Gets or sets the first characters of the current secret. Answered, never read from a request.</summary>
    public string? SecretPrefix { get; set; }

    /// <summary>Gets or sets when the current secret was made. Answered, never read from a request.</summary>
    public DateTimeOffset? SecretCreatedAt { get; set; }
}
