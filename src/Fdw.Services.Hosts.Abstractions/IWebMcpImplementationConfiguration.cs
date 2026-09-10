using Fdw.Configuration;

namespace Fdw.Services.Hosts.Abstractions;

/// <summary>
/// The contract every WebMcp implementation's configuration satisfies -- the Web MCP surface a host exposes.
/// </summary>
public interface IWebMcpImplementationConfiguration : IImplementationConfiguration
{
}
