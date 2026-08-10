namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// Response for ETL webhook acknowledgement.
/// </summary>
public sealed class EtlWebhookResponse
{
    /// <summary>Gets or sets whether the webhook was acknowledged.</summary>
    public bool Acknowledged { get; set; }

    /// <summary>Gets or sets the execution ID that was processed.</summary>
    public string ExecutionId { get; set; } = string.Empty;
}
