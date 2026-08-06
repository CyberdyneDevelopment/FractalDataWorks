namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// Request for getting or deleting a pipeline by name.
/// </summary>
public class PipelineNameRequest
{
    /// <summary>
    /// Gets or sets the pipeline name (from route).
    /// </summary>
    public string Name { get; set; } = string.Empty;
}
