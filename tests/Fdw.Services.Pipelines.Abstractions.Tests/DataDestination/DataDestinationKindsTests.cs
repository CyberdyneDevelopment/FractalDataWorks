using Fdw.Services.Pipelines.Abstractions.DataDestination;
using Shouldly;
using Xunit;

namespace Fdw.Services.Pipelines.Abstractions.Tests.DataDestination;

[Collection(nameof(PipelinesTestCollection))]
public class DataDestinationKindsTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void AllReturnsAllDataDestinationKinds()
    {
        var all = DataDestinationKinds.All();

        all.ShouldNotBeEmpty();
        all.Count.ShouldBeGreaterThanOrEqualTo(2);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ByIdReturnsCorrectDataDestinationKind()
    {
        var result = DataDestinationKinds.ById(1);

        result.ShouldNotBeNull();
        result.Id.ShouldBe(1);
        result.Name.ShouldBe("Connection");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ByIdReturnsNotFoundForUnknownId()
    {
        var result = DataDestinationKinds.ById(99999);

        result.ShouldNotBeNull();
        result.Name.ShouldBe("_Empty");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ByNameReturnsConnectionKind()
    {
        var result = DataDestinationKinds.ByName("Connection");

        result.ShouldNotBeNull();
        result.Id.ShouldBe(1);
        result.Name.ShouldBe("Connection");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ByNameReturnsDataSetKind()
    {
        var result = DataDestinationKinds.ByName("DataSet");

        result.ShouldNotBeNull();
        result.Id.ShouldBe(2);
        result.Name.ShouldBe("DataSet");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ByNameReturnsNotFoundForUnknownName()
    {
        var result = DataDestinationKinds.ByName("Unknown");

        result.ShouldNotBeNull();
        result.Name.ShouldBe("_Empty");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ByNameIsCaseSensitive()
    {
        DataDestinationKinds.ByName("Connection").ShouldNotBeNull();
        DataDestinationKinds.ByName("Connection").Name.ShouldBe("Connection");
        DataDestinationKinds.ByName("connection").Name.ShouldBe("_Empty");
        DataDestinationKinds.ByName("CONNECTION").Name.ShouldBe("_Empty");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void NotFoundReturnsEmptyInstance()
    {
        var result = DataDestinationKinds.NotFound;

        result.ShouldNotBeNull();
        result.Name.ShouldBe("_Empty");
        result.Id.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void AllContainsConnectionAndDataSetKinds()
    {
        var all = DataDestinationKinds.All();

        all.ShouldContain(k => k.Name == "Connection");
        all.ShouldContain(k => k.Name == "DataSet");
    }
}
