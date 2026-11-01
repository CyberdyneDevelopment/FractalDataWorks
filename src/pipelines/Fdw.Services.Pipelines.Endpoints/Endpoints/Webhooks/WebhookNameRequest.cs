namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>Request naming a webhook trigger: the route of the endpoints that act on one.</summary>
public class WebhookNameRequest
{
    /// <summary>Gets or sets the trigger's name, from the route.</summary>
    public string Name { get; set; } = string.Empty;
}
