namespace Fdw.Services.Pipelines.Clients.Abstractions;

/// <summary>
/// Client-side request to read one pipeline: the body of <c>POST /pipelines/get</c>.
/// </summary>
/// <remarks>
/// A read of one record is a POST, not a GET: a GET is a plain paged list and nothing else.
/// </remarks>
public class GetPipelineClientRequest
{
    /// <summary>
    /// Gets or sets the pipeline name.
    /// </summary>
    public string Name { get; set; } = string.Empty;
}
