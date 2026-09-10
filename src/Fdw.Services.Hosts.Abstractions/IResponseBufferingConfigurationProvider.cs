using Fdw.Services.Abstractions;

namespace Fdw.Services.Hosts.Abstractions;

/// <summary>
/// Supplies ResponseBuffering configuration. Registered and resolved as this type -- never as the base.
/// </summary>
public interface IResponseBufferingConfigurationProvider
    : IDomainConfigurationProvider<IResponseBufferingImplementationConfiguration>
{
}
