using Fdw.Services.Abstractions;

namespace Fdw.Services.Hosts.Abstractions;

/// <summary>
/// Supplies SecurityHeaders configuration. Registered and resolved as this type -- never as the base.
/// </summary>
public interface ISecurityHeadersConfigurationProvider
    : IDomainConfigurationProvider<ISecurityHeadersImplementationConfiguration>
{
}
