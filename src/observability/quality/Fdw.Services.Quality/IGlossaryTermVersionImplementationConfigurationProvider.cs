using Fdw.Services.Abstractions;
using Fdw.Services.Quality.Configuration;

namespace Fdw.Services.Quality;

/// <summary>Supplies the GlossaryTermVersion implementation's own configuration (FDW-2331).</summary>
public interface IGlossaryTermVersionImplementationConfigurationProvider
    : IImplementationConfigurationProvider<IGlossaryTermVersionImplementationConfiguration>
{
}
