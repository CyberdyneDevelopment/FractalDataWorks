using Fdw.Configuration;

namespace Fdw.Services.Hosts.Abstractions;

/// <summary>
/// The contract every SecurityHeaders implementation's configuration satisfies -- the security headers a host sets on every response.
/// </summary>
public interface ISecurityHeadersImplementationConfiguration : IImplementationConfiguration
{
}
