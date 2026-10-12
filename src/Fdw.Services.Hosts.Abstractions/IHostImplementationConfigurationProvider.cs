using Fdw.Services.Abstractions;

namespace Fdw.Services.Hosts.Abstractions;

/// <summary>Supplies the Host implementation's own configuration.</summary>
public interface IHostImplementationConfigurationProvider
    : IImplementationConfigurationProvider<IHostImplementationConfiguration>
{
}
