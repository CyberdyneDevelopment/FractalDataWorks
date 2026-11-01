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

    /// <summary>Gets the absolute address this host reaches its own routes on.</summary>
    /// <remarks>
    /// A tool call that arrives over MCP is sent to the tool's own route at this address. It is
    /// configuration and never the incoming request's Host header, which the caller controls. A host
    /// that does not serve MCP leaves it empty; one that does is refused without it.
    /// </remarks>
    string? SelfBaseAddress { get; }
}
