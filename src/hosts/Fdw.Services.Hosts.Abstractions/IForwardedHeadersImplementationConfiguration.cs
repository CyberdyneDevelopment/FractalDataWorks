using Fdw.Configuration;

namespace Fdw.Services.Hosts.Abstractions;

/// <summary>
/// The contract every ForwardedHeaders implementation's configuration satisfies -- how a host trusts the proxy headers in front of it.
/// </summary>
public interface IForwardedHeadersImplementationConfiguration : IImplementationConfiguration
{
}
