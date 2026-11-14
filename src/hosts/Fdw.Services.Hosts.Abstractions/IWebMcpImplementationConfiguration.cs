using Fdw.Configuration;

namespace Fdw.Services.Hosts.Abstractions;

/// <summary>
/// The contract every WebMcp implementation's configuration satisfies -- the Web MCP surface a host exposes.
/// </summary>
public interface IWebMcpImplementationConfiguration : IImplementationConfiguration
{
    /// <summary>Gets the request header the host's client key travels in.</summary>
    /// <remarks>Required: the script and the MCP bridge both name it on every tool call.</remarks>
    string ApiKeyHeader { get; }

    /// <summary>Gets the secret manager that holds the host's client key.</summary>
    /// <remarks>
    /// Named together with <see cref="SecretKeyName"/>, or not at all: a host that names neither serves
    /// no client key, and one without the other names nothing and is refused.
    /// </remarks>
    string? SecretManagerName { get; }

    /// <summary>Gets the client key's name in <see cref="SecretManagerName"/>.</summary>
    string? SecretKeyName { get; }

    /// <summary>Gets the name of the source this host's own address comes from (FDW-5721).</summary>
    /// <remarks>
    /// A tool call that arrives over MCP is sent to the tool's own route at the address this source
    /// gives: <c>ListeningAddress</c>, the address the running process listens on, so two instances on
    /// two ports each call themselves; or <c>FixedAddress</c>, the <see cref="SelfBaseAddress"/> this row
    /// states. It is never the incoming request's Host header, which the caller controls, and nothing is
    /// assumed: a host that serves MCP and names no source is refused. A host that does not serve MCP
    /// leaves this and <see cref="SelfBaseAddress"/> empty.
    /// </remarks>
    string? SelfAddressSource { get; }

    /// <summary>Gets the absolute address this host reaches its own routes on, read by the <c>FixedAddress</c> source.</summary>
    /// <remarks>
    /// Stated only with <see cref="SelfAddressSource"/> <c>FixedAddress</c> (a host behind a proxy). A row that
    /// states it with no source, or with a source that does not read it, is refused.
    /// </remarks>
    string? SelfBaseAddress { get; }
}
