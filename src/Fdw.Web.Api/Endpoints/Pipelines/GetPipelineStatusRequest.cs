namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// Request to get pipeline status.
/// </summary>
public class GetPipelineStatusRequest
{
    /// <summary>
    /// Gets or sets the pipeline name (bound from route).
    /// </summary>
    public string Name { get; set; } = string.Empty;
}
