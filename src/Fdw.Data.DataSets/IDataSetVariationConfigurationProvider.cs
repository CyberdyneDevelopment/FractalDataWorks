using Fdw.Services.Abstractions;
using Fdw.Services.Data.Abstractions;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>Supplies the configured data set variations: a member whole, or a page of them as domain records.</summary>
/// <remarks>
/// The reads that decide something about a variation (which data set it varies, which workspace owns it) are
/// domain-record reads: one query on the domain table, no variation composed.
/// </remarks>
public interface IDataSetVariationConfigurationProvider
    : IDomainConfigurationProvider<IDataSetVariationImplementationConfiguration>,
      IDomainRecordProvider<DataSetVariationDomainConfiguration>
{
}
