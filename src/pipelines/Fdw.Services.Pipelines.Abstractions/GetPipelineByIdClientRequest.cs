using System;

namespace Fdw.Services.Pipelines.Clients.Abstractions;

/// <summary>
/// Client-side request to read one pipeline by its logical id: the body of <c>POST /pipelines/get</c> (FDW-1088).
/// </summary>
/// <remarks>
/// A request names a pipeline by id or by name, never both, so the by-id read is a request of its own rather
/// than an optional field on <see cref="GetPipelineClientRequest"/>.
/// </remarks>
public class GetPipelineByIdClientRequest
{
    /// <summary>Gets or sets the pipeline's logical id.</summary>
    public Guid Id { get; set; }
}
