using Fdw.Configuration;

namespace Fdw.Services.Hosts.Abstractions;

/// <summary>
/// The contract every CORS implementation's configuration satisfies.
/// </summary>
/// <remarks>
/// CORS is its own domain rather than one option of a host, because a host runs CORS alongside
/// security headers and forwarded headers rather than instead of them — a domain names one
/// implementation, and "the host implementation" was never singular.
/// </remarks>
public interface ICorsImplementationConfiguration : IImplementationConfiguration
{
}
