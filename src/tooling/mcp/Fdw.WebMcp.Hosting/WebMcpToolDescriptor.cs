using System;
using Microsoft.AspNetCore.Http;
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
    Type? ResponseType,
    string? ParentListRoute = null,
    string? ParentListToolName = null,
    string? PresetFieldName = null,
    string? PresetValue = null)
{
    /// <summary>
    /// The route endpoint the tool was joined to, whose authorization metadata decides which callers
    /// are offered the tool over MCP; <see langword="null"/> for a descriptor made without a route table.
    /// </summary>
    public Endpoint? Endpoint { get; init; }
}
