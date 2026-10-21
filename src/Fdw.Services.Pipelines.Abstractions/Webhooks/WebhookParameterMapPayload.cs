namespace Fdw.Services.Pipelines.Clients.Abstractions;

/// <summary>One mapping of a webhook trigger: the value at a path in the call's JSON body becomes a pipeline parameter.</summary>
public class WebhookParameterMapPayload
{
    /// <summary>Gets or sets the dotted path of property names in the body (<c>order.region</c>).</summary>
    public string BodyPath { get; set; } = string.Empty;

    /// <summary>Gets or sets the name of the pipeline parameter the value is given to.</summary>
    public string Parameter { get; set; } = string.Empty;
}
