using Fdw.Services.Abstractions;

namespace Fdw.Services.Hosts.Abstractions;

/// <summary>
/// Supplies EmptyBody configuration. Registered and resolved as this type -- never as the base.
/// </summary>
public interface IEmptyBodyConfigurationProvider
    : IImplementationConfigurationProvider<IEmptyBodyImplementationConfiguration>
{
}
