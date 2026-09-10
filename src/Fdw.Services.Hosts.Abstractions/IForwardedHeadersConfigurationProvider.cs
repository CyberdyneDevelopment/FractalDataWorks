using Fdw.Services.Abstractions;

namespace Fdw.Services.Hosts.Abstractions;

/// <summary>
/// Supplies ForwardedHeaders configuration. Registered and resolved as this type -- never as the base.
/// </summary>
public interface IForwardedHeadersConfigurationProvider
    : IImplementationConfigurationProvider<IForwardedHeadersImplementationConfiguration>
{
}
