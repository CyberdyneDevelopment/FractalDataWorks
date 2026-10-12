using System;

namespace Fdw.WebMcp.Hosting;

/// <summary>
/// What a host's WebMCP surface needs, read from its WebMcp configuration row and resolved.
/// </summary>
/// <param name="ApiKeyHeader">The request header the client key travels in.</param>
/// <param name="ClientApiKey">
/// The host's client key, resolved from the secret manager the row names, or <see langword="null"/>
/// when the row names none and the host serves no client key.
/// </param>
/// <param name="SelfBaseAddress">
/// The address the host reaches its own routes on, or <see langword="null"/> when the row names none
/// and the host does not serve MCP.
/// </param>
public sealed record WebMcpHostSettings(string ApiKeyHeader, string? ClientApiKey, Uri? SelfBaseAddress);
