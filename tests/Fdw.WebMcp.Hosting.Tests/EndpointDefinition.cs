using System;

namespace Fdw.WebMcp.Hosting.Tests;

/// <summary>
/// Stands in for FastEndpoints' <c>EndpointDefinition</c> metadata.
/// </summary>
/// <remarks>
/// Named <c>EndpointDefinition</c> on purpose. The registry finds this metadata by type NAME so the
/// package needs no FastEndpoints dependency, and a stub that matched by anything else would leave
/// exactly that lookup untested — the part most likely to break when FastEndpoints moves the type.
/// </remarks>
internal sealed class EndpointDefinition
{
    public Type? EndpointType { get; init; }

    public Type? ReqDtoType { get; init; }

    public Type? ResDtoType { get; init; }
}
