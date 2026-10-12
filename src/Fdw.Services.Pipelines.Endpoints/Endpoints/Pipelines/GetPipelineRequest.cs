using System;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// The body of <c>POST /pipelines/get</c>: the pipeline to read, named by exactly one of its logical id or its name
/// (FDW-1088).
/// </summary>
/// <remarks>
/// The id is what another service holds -- a schedule stores the pipeline it runs by id, never by a name another
/// tenant may share. A request naming both, or neither, is refused.
/// </remarks>
public class GetPipelineRequest
{
    /// <summary>Gets or sets the pipeline name.</summary>
    public string? Name { get; set; }

    /// <summary>Gets or sets the pipeline's logical id.</summary>
    public Guid? Id { get; set; }
}
