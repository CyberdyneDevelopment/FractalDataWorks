namespace Fdw.Services.Pipelines.Clients.Abstractions;

/// <summary>The answer to a relayed pipeline event: the kind the receiving host broadcast.</summary>
public sealed class PipelineEventResponse
{
    /// <summary>Gets or sets the event kind that was broadcast.</summary>
    public string Kind { get; set; } = string.Empty;
}
