using Fdw.Services.Quality.Configuration;
using Fdw.Services.Abstractions;

namespace Fdw.Services.Quality;

/// <summary>Supplies the configured Environment members.</summary>
public interface IEnvironmentConfigurationProvider
    : IDomainConfigurationProvider<IEnvironmentImplementationConfiguration>
{
}
