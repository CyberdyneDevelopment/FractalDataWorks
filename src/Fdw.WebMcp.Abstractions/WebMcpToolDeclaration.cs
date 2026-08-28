using System;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.WebMcp.Abstractions;

/// <summary>
/// What an endpoint option declared about itself as a WebMCP tool, before the route is known.
/// </summary>
/// <remarks>
/// The half of a tool that is decided at declaration time. The other half — route and verb — comes
/// from the router at <c>MapWebMcp</c>, which is why this is a separate type from the descriptor
/// that is finally served rather than one type with the route left null in between.
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed record WebMcpToolDeclaration(
    Type EndpointType,
    string Name,
    string Description,
    bool ReadOnly,
    string? HttpMethodOverride);
