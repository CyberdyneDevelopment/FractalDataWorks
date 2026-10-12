using System;
using FastEndpoints;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>Request for <c>GET /map</c>: which part of the estate to draw.</summary>
/// <remarks>
/// Neither parameter draws everything the caller can see. Naming both is refused, never answered with one of them: a map
/// of an organization's pipelines inside a workspace is not a scope the map has. Changing tenant is not a parameter here: a
/// token acts in one tenant, so switching tenant is switching the active tenant on the token.
/// </remarks>
public sealed class MapRequest
{
    /// <summary>Gets or sets the name of the workspace to draw. The caller must be allowed to view it.</summary>
    [QueryParam]
    public string? Workspace { get; set; }

    /// <summary>Gets or sets the id of the organization whose pipelines, and what they reach, to draw.</summary>
    [QueryParam]
    public Guid? Organization { get; set; }

    /// <summary>Gets or sets the most nodes to send, when the caller wants a cap. Omitted means no cap. Honoured when the response is the line form (<c>Accept: application/x-ndjson</c>).</summary>
    /// <remarks>A cap that is reached is never silent: the <c>end</c> line says <c>truncated</c>, with the cap.</remarks>
    [QueryParam]
    public int? MaxNodes { get; set; }
}
