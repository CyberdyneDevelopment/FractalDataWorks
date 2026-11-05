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
/// A variation is a data set of kind Variation (FDW-3638). The list record is read from the <c>data.DataSetVariationList</c> view and the
/// whole variation from <c>data.VariationDataSetConfiguration</c>; the two have to agree on the names of what a list shows, or a list
/// and the member it names disagree about the same variation.
/// </summary>
public class DataSetVariationDomainConfigurationTests
{
    private static readonly string[] ListFields =
        ["VariationName", "VariationKind", "DisplayName", "Description", "OwnerWorkspaceId", "OwnerUserId", "BaseDataSetId", "ParentVariationId", "StoredFilters", "ClassificationId", "CreatedAt"];

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public void TheListRecordHoldsWhatAListOfVariationsShows()
    {
        var record = PocoMapperCollection.ByName(nameof(DataSetVariationDomainConfiguration));
        var plain = PocoMapperCollection.ByName(nameof(DomainConfiguration));

        record.GetPropertyNames().Except(plain.GetPropertyNames()).ShouldBe(ListFields, ignoreOrder: true);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public void EveryStoredListFieldIsAlsoAPropertyOfTheVariationDataSet()
    {
        var variation = PocoMapperCollection.ByName(nameof(VariationDataSetConfiguration));

        // VariationKind is told from the owner and is not stored.
        ListFields.Where(field => field != "VariationKind").Except(variation.GetPropertyNames()).ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public void TheVariationDataSetHoldsTheChartBesideTheList()
    {
        var variation = PocoMapperCollection.ByName(nameof(VariationDataSetConfiguration));

        variation.GetPropertyNames().ShouldContain("Encoding");
        variation.GetPropertyNames().ShouldContain("ChartType");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public void AVariationIsNotYetClassifiedUntilSomeoneClassifiesIt()
    {
        new DataSetVariationDomainConfiguration().ClassificationId.ShouldBe(DataClassifications.NotYetClassified.Id);
        new VariationDataSetConfiguration().ClassificationId.ShouldBe(DataClassifications.NotYetClassified.Id);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public void AVariationNamesItselfByTheImplementationItsDataSetRowCarries()
    {
        VariationDataSetConfiguration.ImplementationName.ShouldBe("Variation");
        VariationKinds.Scoped.ShouldBe("Scoped");
        VariationKinds.DataView.ShouldBe("DataView");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public void TheOwnerTellsAScopedVariationFromADataView()
    {
        new VariationDataSetConfiguration { OwnerWorkspaceId = Guid.NewGuid() }.VariationKind.ShouldBe(VariationKinds.Scoped);
        new VariationDataSetConfiguration { OwnerUserId = Guid.NewGuid() }.VariationKind.ShouldBe(VariationKinds.DataView);
        new VariationDataSetConfiguration().VariationKind.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public void AVariationIsADataSetThatHoldsNoFieldsOfItsOwn()
    {
        var variation = new VariationDataSetConfiguration();

        variation.ShouldBeAssignableTo<IDataSetImplementationConfiguration>();
        variation.Fields.ShouldBeEmpty();
        variation.Sources.ShouldBeEmpty();
        variation.KeyFields.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public void StoredListFieldsCarryFromTheListRecordOntoTheVariationAndBack()
    {
        var owner = Guid.NewGuid();
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
            BaseDataSetId = main,
            ParentVariationId = parent,
            StoredFilters = filters,
            ClassificationId = 123456,
            CreatedAt = created,
        };
        var rowMapper = PocoMapperCollection.ByName(nameof(DataSetVariationDomainConfiguration));
        var implementationMapper = PocoMapperCollection.ByName(nameof(VariationDataSetConfiguration));
        var implementation = new VariationDataSetConfiguration();
        var stored = ListFields.Where(field => field != "VariationKind").ToList();

        var values = rowMapper.MapToParameters(row);
        foreach (var field in stored)
            implementationMapper.SetValue(implementation, field, values[field]);

        implementation.VariationName.ShouldBe("students");
        implementation.DisplayName.ShouldBe("Students");
        implementation.Description.ShouldBe("A workspace's view");
        implementation.OwnerWorkspaceId.ShouldBe(owner);
        implementation.BaseDataSetId.ShouldBe(main);
        implementation.ParentVariationId.ShouldBe(parent);
        implementation.StoredFilters.ShouldBe(filters);
        implementation.ClassificationId.ShouldBe(123456);
        implementation.CreatedAt.ShouldBe(created);
        implementation.VariationKind.ShouldBe(VariationKinds.Scoped);

        var taken = new DataSetVariationDomainConfiguration();
        var carried = implementationMapper.MapToParameters(implementation);
        foreach (var field in stored)
            rowMapper.SetValue(taken, field, carried[field]);

        taken.VariationName.ShouldBe("students");
        taken.OwnerWorkspaceId.ShouldBe(owner);
        taken.BaseDataSetId.ShouldBe(main);
        taken.ParentVariationId.ShouldBe(parent);
        taken.StoredFilters.ShouldBe(filters);
        taken.ClassificationId.ShouldBe(123456);
        taken.CreatedAt.ShouldBe(created);
    }
}
