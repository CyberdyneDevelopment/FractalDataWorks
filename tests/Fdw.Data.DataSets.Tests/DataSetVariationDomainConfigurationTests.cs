using System;
using System.Linq;
using Fdw.Configuration;
using Fdw.Data.Abstractions;
using Fdw.Data.Abstractions.Mappers.PocoMappers;
using Fdw.Data.DataSets.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Data.DataSets.Tests;

/// <summary>
/// The domain provider copies a record's properties beyond the four every domain has onto the implementation it
/// composes, and back on save, by mapper property name. A name the implementation lacks fails every read of every
/// variation, so the domain record and each kind's implementation have to agree.
/// </summary>
public class DataSetVariationDomainConfigurationTests
{
    private static readonly string[] ListFields =
        ["VariationName", "DisplayName", "Description", "OwnerWorkspaceId", "OwnerUserId", "BaseDataSetId", "ParentVariationId", "Filters", "ClassificationId", "CreatedAt"];

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public void TheDomainRecordListsWhatAVariationShows()
    {
        var record = PocoMapperCollection.ByName(nameof(DataSetVariationDomainConfiguration));
        var plain = PocoMapperCollection.ByName(nameof(DomainConfiguration));

        record.GetPropertyNames().Except(plain.GetPropertyNames()).ShouldBe(ListFields, ignoreOrder: true);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public void EveryListFieldIsAlsoAPropertyOfEachKindsImplementation()
    {
        var scoped = PocoMapperCollection.ByName(nameof(ScopedDataSetVariationConfiguration));
        var dataView = PocoMapperCollection.ByName(nameof(DataViewDataSetVariationConfiguration));

        ListFields.Except(scoped.GetPropertyNames()).ShouldBeEmpty();
        ListFields.Except(dataView.GetPropertyNames()).ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public void ADataViewImplementationHoldsTheChartAndAScopedOneHoldsNothingBeyondTheList()
    {
        var scoped = PocoMapperCollection.ByName(nameof(ScopedDataSetVariationConfiguration));
        var dataView = PocoMapperCollection.ByName(nameof(DataViewDataSetVariationConfiguration));

        dataView.GetPropertyNames().ShouldContain("Encoding");
        dataView.GetPropertyNames().ShouldContain("ChartType");
        scoped.GetPropertyNames().ShouldNotContain("Encoding");
        scoped.GetPropertyNames().ShouldNotContain("ChartType");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public void AVariationIsNotYetClassifiedUntilSomeoneClassifiesIt()
    {
        new DataSetVariationDomainConfiguration().ClassificationId.ShouldBe(DataClassifications.NotYetClassified.Id);
        new ScopedDataSetVariationConfiguration().ClassificationId.ShouldBe(DataClassifications.NotYetClassified.Id);
        new DataViewDataSetVariationConfiguration().ClassificationId.ShouldBe(DataClassifications.NotYetClassified.Id);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public void EachKindNamesItselfByTheImplementationItsDomainRowCarries()
    {
        ScopedDataSetVariationConfiguration.ImplementationName.ShouldBe("Scoped");
        DataViewDataSetVariationConfiguration.ImplementationName.ShouldBe("DataView");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public void ListFieldsCarryFromTheDomainRowOntoTheImplementationAndBack()
    {
        var owner = Guid.NewGuid();
        var user = Guid.NewGuid();
        var main = Guid.NewGuid();
        var parent = Guid.NewGuid();
        const string filters = """{"field":"grade","operator":"Equal","value":"07"}""";
        var created = DateTimeOffset.UtcNow;
        var row = new DataSetVariationDomainConfiguration
        {
            VariationName = "students",
            DisplayName = "Students",
            Description = "A workspace's view",
            OwnerWorkspaceId = owner,
            OwnerUserId = user,
            BaseDataSetId = main,
            ParentVariationId = parent,
            Filters = filters,
            ClassificationId = 123456,
            CreatedAt = created,
        };
        var rowMapper = PocoMapperCollection.ByName(nameof(DataSetVariationDomainConfiguration));
        var implementationMapper = PocoMapperCollection.ByName(nameof(ScopedDataSetVariationConfiguration));
        var implementation = new ScopedDataSetVariationConfiguration();

        var values = rowMapper.MapToParameters(row);
        foreach (var field in ListFields)
            implementationMapper.SetValue(implementation, field, values[field]);

        implementation.VariationName.ShouldBe("students");
        implementation.DisplayName.ShouldBe("Students");
        implementation.Description.ShouldBe("A workspace's view");
        implementation.OwnerWorkspaceId.ShouldBe(owner);
        implementation.OwnerUserId.ShouldBe(user);
        implementation.BaseDataSetId.ShouldBe(main);
        implementation.ParentVariationId.ShouldBe(parent);
        implementation.Filters.ShouldBe(filters);
        implementation.ClassificationId.ShouldBe(123456);
        implementation.CreatedAt.ShouldBe(created);

        var taken = new DataSetVariationDomainConfiguration();
        var carried = implementationMapper.MapToParameters(implementation);
        foreach (var field in ListFields)
            rowMapper.SetValue(taken, field, carried[field]);

        taken.VariationName.ShouldBe("students");
        taken.OwnerWorkspaceId.ShouldBe(owner);
        taken.OwnerUserId.ShouldBe(user);
        taken.BaseDataSetId.ShouldBe(main);
        taken.ParentVariationId.ShouldBe(parent);
        taken.Filters.ShouldBe(filters);
        taken.ClassificationId.ShouldBe(123456);
        taken.CreatedAt.ShouldBe(created);
    }
}
