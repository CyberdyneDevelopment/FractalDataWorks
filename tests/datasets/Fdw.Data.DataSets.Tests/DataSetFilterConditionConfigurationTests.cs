using System;
using System.Linq;
using Fdw.Data.DataSets.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Data.DataSets.Tests;

/// <summary>
/// A data set's own filter conditions are a saved child of the data set (data.DataSetFilter, FDW-4997). Before, the list held a type that was
/// not a configuration, so the generated mapper skipped it and the filters a request sent were never saved.
/// </summary>
public class DataSetFilterConditionConfigurationTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataSets.FDW4997")]
    public void SimpleCompoundAndFederatedMappersCascadeFiltersAsTypedChildList()
    {
        foreach (var mapper in new[]
        {
            new SimpleDataSetConfigurationPocoMapper().CascadeChildren,
            new CompoundDataSetConfigurationPocoMapper().CascadeChildren,
            new FederatedDataSetConfigurationPocoMapper().CascadeChildren,
        })
        {
            var filters = mapper.SingleOrDefault(c => string.Equals(c.BoundPropertyName, nameof(IDataSetImplementationConfiguration.Filters), StringComparison.Ordinal));

            filters.ShouldNotBeNull();
            filters.ChildType.ShouldBe(typeof(DataSetFilterConditionConfiguration));
        }
    }
}
