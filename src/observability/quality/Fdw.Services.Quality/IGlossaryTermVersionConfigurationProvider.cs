using Fdw.Services.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Quality.Configuration;

namespace Fdw.Services.Quality;

/// <summary>Reads and writes the versions of glossary terms held at places (FDW-2331), by name, by id, or a page from the domain table alone.</summary>
public interface IGlossaryTermVersionConfigurationProvider
    : IDomainConfigurationProvider<IGlossaryTermVersionImplementationConfiguration>,
      IDomainRecordProvider<GlossaryTermVersionDomainConfiguration>
{
}
