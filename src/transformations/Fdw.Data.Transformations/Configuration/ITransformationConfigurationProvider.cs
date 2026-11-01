using Fdw.Services.Abstractions;

namespace Fdw.Data.Transformations.Configuration;

/// <summary>Supplies the configured transformations.</summary>
public interface ITransformationConfigurationProvider
    : IDomainConfigurationProvider<ITransformationImplementationConfiguration>
{
}
