using Fdw.Services.Pipelines.Abstractions.DataSource;
using Shouldly;
using Xunit;

namespace Fdw.Services.Pipelines.Abstractions.Tests.DataSource;

[Collection(nameof(PipelinesTestCollection))]
public class DataSourceKindsTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void AllReturnsAllDataSourceKinds()
    {
        var all = DataSourceKinds.All();

        all.ShouldNotBeEmpty();
        all.Count.ShouldBeGreaterThanOrEqualTo(2);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ByIdReturnsCorrectDataSourceKind()
    {
        var result = DataSourceKinds.ById(1);

        result.ShouldNotBeNull();
        result.Id.ShouldBe(1);
        result.Name.ShouldBe("Connection");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ByIdReturnsNotFoundForUnknownId()
    {
        var result = DataSourceKinds.ById(99999);

        result.ShouldNotBeNull();
        result.Name.ShouldBe("_Empty");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ByNameReturnsConnectionKind()
    {
        var result = DataSourceKinds.ByName("Connection");

        result.ShouldNotBeNull();
        result.Id.ShouldBe(1);
        result.Name.ShouldBe("Connection");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ByNameReturnsDataSetKind()
    {
        var result = DataSourceKinds.ByName("DataSet");

        result.ShouldNotBeNull();
        result.Id.ShouldBe(2);
        result.Name.ShouldBe("DataSet");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ByNameReturnsNotFoundForUnknownName()
    {
        var result = DataSourceKinds.ByName("Unknown");

        result.ShouldNotBeNull();
        result.Name.ShouldBe("_Empty");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ByNameIsCaseSensitive()
    {
        DataSourceKinds.ByName("Connection").ShouldNotBeNull();
        DataSourceKinds.ByName("Connection").Name.ShouldBe("Connection");
        DataSourceKinds.ByName("connection").Name.ShouldBe("_Empty");
        DataSourceKinds.ByName("CONNECTION").Name.ShouldBe("_Empty");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void NotFoundReturnsEmptyInstance()
    {
        var result = DataSourceKinds.NotFound;

        result.ShouldNotBeNull();
        result.Name.ShouldBe("_Empty");
        result.Id.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void AllContainsConnectionAndDataSetKinds()
    {
        var all = DataSourceKinds.All();

        all.ShouldContain(k => k.Name == "Connection");
        all.ShouldContain(k => k.Name == "DataSet");
    }
}
