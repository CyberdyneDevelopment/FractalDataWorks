using Fdw.Services.Data.Abstractions;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>Supplies a page of data sets as the records their list shows: name, display name and abbreviation (FDW-6129).</summary>
/// <remarks>
/// Beside <see cref="IDataSetConfigurationProvider"/>, which keeps reading plain domain records, so a caller that needs only a name and an
/// Id is not changed. Asked for a search, a page and a total in one query on the domain table, with no implementation consulted.
/// </remarks>
public interface IDataSetRecordProvider : IDomainRecordProvider<DataSetDomainConfiguration>
{
}
