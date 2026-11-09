using Fdw.Configuration;
using Fdw.Services.Abstractions;
using Fdw.Services.Data.Abstractions;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>Supplies the configured data sets.</summary>
/// <remarks>
/// The DataSet domain's configuration interface. Distinct from IDataSetProvider, which hands out
/// runtime data sets, and from DataSetProvider, which is the domain's platform service provider
/// and reads its configuration through this. A list or lookup of data sets reads a page of domain records,
/// one query on the domain table (FDW-4636).
/// </remarks>
public interface IDataSetConfigurationProvider
    : IDomainConfigurationProvider<IDataSetImplementationConfiguration>,
      IDomainRecordProvider<DomainConfiguration>
{
}
