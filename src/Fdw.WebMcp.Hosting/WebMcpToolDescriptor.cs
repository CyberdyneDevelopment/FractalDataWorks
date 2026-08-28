using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.WebMcp.Abstractions;

namespace Fdw.WebMcp.Hosting;

/// <summary>
/// A WebMCP tool ready to be served: what the endpoint option declared in
/// <see cref="WebMcpToolAttribute"/>, joined with the route the application actually serves it on.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed record WebMcpToolDescriptor(
    string Name,
    string Description,
    string Route,
    string HttpMethod,
    bool ReadOnly,
    Type? RequestType,
    Type? ResponseType);
