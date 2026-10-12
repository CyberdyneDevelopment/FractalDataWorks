namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// Request naming a pipeline: the body of <c>POST /pipelines/get</c>, or the route of the endpoints that act on one.
/// </summary>
public class PipelineNameRequest
{
    /// <summary>
    /// Gets or sets the pipeline name (from the body, or from the route).
    /// </summary>
    public string Name { get; set; } = string.Empty;
}
