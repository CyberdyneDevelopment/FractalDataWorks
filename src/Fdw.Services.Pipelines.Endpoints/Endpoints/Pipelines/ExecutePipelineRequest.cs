namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// Request to execute a pipeline by name.
/// </summary>
public class ExecutePipelineRequest
{
    /// <summary>
    /// Gets or sets the pipeline name (bound from route).
    /// </summary>
    public string Name { get; set; } = string.Empty;
}
