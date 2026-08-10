namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// Response for pipeline status.
/// </summary>
public class GetPipelineStatusResponse
{
    /// <summary>
    /// Gets or sets whether the pipeline was found.
    /// </summary>
    public bool Found { get; set; }

    /// <summary>
    /// Gets or sets the pipeline information.
    /// </summary>
    public PipelineStatusInfo? Pipeline { get; set; }

    /// <summary>
    /// Gets or sets the result message.
    /// </summary>
    public string? Message { get; set; }
}
