namespace Fdw.Services.Pipelines.Clients.Abstractions;

/// <summary>One row of the list of webhook triggers.</summary>
public class WebhookSummaryPayload
{
    /// <summary>Gets or sets the trigger's name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the name of the pipeline the trigger starts.</summary>
    public string Pipeline { get; set; } = string.Empty;

    /// <summary>Gets or sets whether the trigger is on.</summary>
    public bool Enabled { get; set; }

    /// <summary>Gets or sets what the trigger is for.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the first characters of the current secret.</summary>
    public string SecretPrefix { get; set; } = string.Empty;
}
