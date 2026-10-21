using Fdw.Services.Abstractions;
using Fdw.Services.Data.Abstractions;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>Supplies the categories: a node whole, or the whole tree as domain records (FDW-1465).</summary>
/// <remarks>The tree is a domain-record read: one query on the domain table, no node composed.</remarks>
public interface ICategoryConfigurationProvider
    : IDomainConfigurationProvider<ICategoryImplementationConfiguration>,
      IDomainRecordProvider<CategoryDomainConfiguration>
{
}
