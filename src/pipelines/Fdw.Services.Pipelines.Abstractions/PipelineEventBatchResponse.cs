namespace Fdw.Services.Pipelines.Clients.Abstractions;

/// <summary>The answer to a relayed batch of pipeline events: how many the receiving host broadcast.</summary>
public sealed class PipelineEventBatchResponse
{
    /// <summary>Gets or sets how many events of the batch were broadcast.</summary>
    public int Relayed { get; set; }
}
