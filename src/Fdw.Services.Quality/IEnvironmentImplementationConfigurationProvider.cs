using Fdw.Services.Abstractions;
using Fdw.Services.Quality.Configuration;

namespace Fdw.Services.Quality;

/// <summary>Supplies the Environment implementation's own configuration to the domain that registers it.</summary>
public interface IEnvironmentImplementationConfigurationProvider
    : IImplementationConfigurationProvider<IEnvironmentImplementationConfiguration>
{
}
