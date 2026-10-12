using System;
using Fdw.Services.Hosts.Abstractions;

namespace Fdw.WebMcp.Mcp.Tests;

/// <summary>A host's WebMcp configuration row, as a test states it.</summary>
internal sealed class McpRow : IWebMcpImplementationConfiguration
{
    public Guid Id { get; set; } = new("8d3f5b2e-1c4a-4f6e-b7d9-3e2a1c0f5d44");

    public string Name { get; set; } = "WebMcp";

    public string Domain { get; set; } = "WebMcp";

    public string Implementation { get; set; } = "Host";

    public string ApiKeyHeader { get; set; } = "X-Webmcp-Key";

    public string? SecretManagerName { get; set; }

    public string? SecretKeyName { get; set; }

    public string? SelfBaseAddress { get; set; }
}
