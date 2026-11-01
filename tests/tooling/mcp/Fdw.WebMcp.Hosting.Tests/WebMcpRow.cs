using System;
using Fdw.Services.Hosts.Abstractions;

namespace Fdw.WebMcp.Hosting.Tests;

/// <summary>A host's WebMcp configuration row, as a test states it.</summary>
internal sealed class WebMcpRow : IWebMcpImplementationConfiguration
{
    public Guid Id { get; set; } = new("5e0c1f3a-6d1b-4c1e-9a58-2f0a7a4f0b11");

    public string Name { get; set; } = "WebMcp";

    public string Domain { get; set; } = "WebMcp";

    public string Implementation { get; set; } = "Host";

    public string ApiKeyHeader { get; set; } = "X-Webmcp-Key";

    public string? SecretManagerName { get; set; }

    public string? SecretKeyName { get; set; }

    public string? SelfBaseAddress { get; set; }
}
