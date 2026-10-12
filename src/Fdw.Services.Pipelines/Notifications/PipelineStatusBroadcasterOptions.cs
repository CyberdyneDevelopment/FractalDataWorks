namespace Fdw.Services.Pipelines.Notifications;

/// <summary>
/// Options for the <see cref="PipelineStatusBroadcaster"/>.
/// Bound from the <c>PipelineTestMode</c> appsettings section.
/// </summary>
public sealed class PipelineStatusBroadcasterOptions
{
    /// <summary>Gets or sets the broadcast frequency in Hz. Default: 5.</summary>
    public int BroadcastHz { get; set; } = 5;

    /// <summary>Gets or sets the sample buffer max bytes. Default: 10 MB.</summary>
    public long SampleBufferMaxBytes { get; set; } = 10_000_000;
}
