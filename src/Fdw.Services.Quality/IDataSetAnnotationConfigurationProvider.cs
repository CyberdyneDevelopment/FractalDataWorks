using Fdw.Services.Quality.Configuration;
using Fdw.Services.Abstractions;

namespace Fdw.Services.Quality;

/// <summary>Supplies the configured DataSetAnnotation members.</summary>
public interface IDataSetAnnotationConfigurationProvider
    
    : IDomainConfigurationProvider<IDataSetAnnotationImplementationConfiguration>
{
}
