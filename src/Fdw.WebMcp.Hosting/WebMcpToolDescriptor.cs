using System;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.WebMcp.Hosting;

/// <summary>
/// Internal DTO representing a discovered WebMCP tool derived from an endpoint decorated with
/// <see cref="WebMcpToolAttribute"/>.
/// </summary>
// Why: pure positional-record DTO, no logic.
[ExcludeFromCodeCoverage]
public sealed record WebMcpToolDescriptor(
    string Name,
    string Description,
    string Route,
    string HttpMethod,
    bool ReadOnly,
    Type? RequestType,
    Type? ResponseType);
