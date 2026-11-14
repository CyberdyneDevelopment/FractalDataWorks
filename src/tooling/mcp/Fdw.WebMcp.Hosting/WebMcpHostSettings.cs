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
/// <param name="SelfAddressSource">
/// Where the host's own address comes from, the source the row names (FDW-5721), or <see langword="null"/>
/// when the row names none and the host does not serve MCP.
/// </param>
/// <param name="SelfBaseAddress">
/// The address the row states for the host's own routes, read only by the <c>FixedAddress</c> source;
/// <see langword="null"/> for every other source and when the host does not serve MCP.
/// </param>
public sealed record WebMcpHostSettings(
    string ApiKeyHeader,
    string? ClientApiKey,
    WebMcpSelfAddressSourceBase? SelfAddressSource,
    Uri? SelfBaseAddress);
