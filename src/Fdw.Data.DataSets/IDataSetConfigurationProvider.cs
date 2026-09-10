using Fdw.Services.Abstractions;

namespace Fdw.Data.DataSets;

/// <summary>Supplies the configured data sets.</summary>
/// <remarks>
/// The DataSet domain's configuration interface. Distinct from IDataSetProvider, which hands out
/// runtime data sets, and from DataSetProvider, which is the domain's platform service provider
/// and reads its configuration through this.
/// </remarks>
public interface IDataSetConfigurationProvider
    : IDomainConfigurationProvider<IDataSetImplementationConfiguration>
{
}
