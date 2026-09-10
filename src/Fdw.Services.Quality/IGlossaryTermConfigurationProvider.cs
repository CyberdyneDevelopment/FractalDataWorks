using Fdw.Services.Quality.Configuration;
using Fdw.Services.Abstractions;

namespace Fdw.Services.Quality;

/// <summary>Supplies the configured GlossaryTerm members.</summary>
public interface IGlossaryTermConfigurationProvider
    : IDomainConfigurationProvider<IGlossaryTermImplementationConfiguration>
{
}
