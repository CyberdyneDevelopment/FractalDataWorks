using System;
using System.Linq;
using Fdw.Data.DataSets.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Data.DataSets.Tests;

/// <summary>
/// A dataset's joins are a saved child of the dataset (data.DataSetJoin). Before FDW-922 the list held
/// a type that was not a configuration, so the generated mapper skipped it and joins were never saved.
/// </summary>
public class DataSetJoinConfigurationTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void CompoundAndFederatedMappersCascadeJoinsAsTypedChildList()
    {
        foreach (var mapper in new[] { new CompoundDataSetConfigurationPocoMapper().CascadeChildren, new FederatedDataSetConfigurationPocoMapper().CascadeChildren })
        {
            var joins = mapper.SingleOrDefault(c => string.Equals(c.BoundPropertyName, nameof(IJoinedDataSetConfiguration.Joins), StringComparison.Ordinal));

            joins.ShouldNotBeNull();
            joins.ChildType.ShouldBe(typeof(DataSetJoinConfiguration));
            joins.IsPropertyCollection.ShouldBeFalse();
        }
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void ASimpleDataSetHasNoJoinsAndCascadesItsAggregates()
    {
        var children = new SimpleDataSetConfigurationPocoMapper().CascadeChildren;

        children.Any(c => string.Equals(c.BoundPropertyName, nameof(IJoinedDataSetConfiguration.Joins), StringComparison.Ordinal)).ShouldBeFalse();
        children.Any(c => c.ChildType == typeof(DataSetAggregateConfiguration)).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void JoinMapperCarriesTheDataSetJoinColumns()
    {
        var columns = new DataSetJoinConfigurationPocoMapper().GetPropertyNames();

        columns.ShouldContain("LeftSourceName");
        columns.ShouldContain("LeftFieldName");
        columns.ShouldContain("RightSourceName");
        columns.ShouldContain("RightFieldName");
        columns.ShouldContain("JoinType");
        columns.ShouldContain("Ordinal");
    }
}
