namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// Request for triggering an ETL job via proxy.
/// </summary>
public sealed class ProxyTriggerEtlRequest
{
    /// <summary>Gets or sets the pipeline name to execute.</summary>
    public string PipelineName { get; set; } = string.Empty;

    /// <summary>Gets or sets an optional correlation ID for distributed tracing.</summary>
    public string? CorrelationId { get; set; }

    /// <summary>Gets or sets the trigger source identifier.</summary>
    public string? TriggerSource { get; set; }
}
