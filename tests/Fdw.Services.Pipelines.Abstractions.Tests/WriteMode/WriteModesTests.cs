using Fdw.Services.Pipelines.Abstractions.WriteMode;
using Shouldly;
using Xunit;

namespace Fdw.Services.Pipelines.Abstractions.Tests.WriteMode;

[Collection(nameof(PipelinesTestCollection))]
public class WriteModesTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void AllReturnsAllWriteModes()
    {
        var all = WriteModes.All();

        all.ShouldNotBeEmpty();
        all.Count.ShouldBeGreaterThanOrEqualTo(4);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ByIdReturnsInsertMode()
    {
        var result = WriteModes.ById(1);

        result.ShouldNotBeNull();
        result.Id.ShouldBe(1);
        result.Name.ShouldBe("Insert");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ByIdReturnsUpsertMode()
    {
        var result = WriteModes.ById(2);

        result.ShouldNotBeNull();
        result.Id.ShouldBe(2);
        result.Name.ShouldBe("Upsert");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ByIdReturnsReplaceMode()
    {
        var result = WriteModes.ById(3);

        result.ShouldNotBeNull();
        result.Id.ShouldBe(3);
        result.Name.ShouldBe("Replace");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ByIdReturnsAppendMode()
    {
        var result = WriteModes.ById(4);

        result.ShouldNotBeNull();
        result.Id.ShouldBe(4);
        result.Name.ShouldBe("Append");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ByIdReturnsNotFoundForUnknownId()
    {
        var result = WriteModes.ById(99999);

        result.ShouldNotBeNull();
        result.Name.ShouldBe("_Empty");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ByNameReturnsInsertMode()
    {
        var result = WriteModes.ByName("Insert");

        result.ShouldNotBeNull();
        result.Id.ShouldBe(1);
        result.Name.ShouldBe("Insert");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ByNameReturnsUpsertMode()
    {
        var result = WriteModes.ByName("Upsert");

        result.ShouldNotBeNull();
        result.Id.ShouldBe(2);
        result.Name.ShouldBe("Upsert");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ByNameReturnsReplaceMode()
    {
        var result = WriteModes.ByName("Replace");

        result.ShouldNotBeNull();
        result.Id.ShouldBe(3);
        result.Name.ShouldBe("Replace");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ByNameReturnsAppendMode()
    {
        var result = WriteModes.ByName("Append");

        result.ShouldNotBeNull();
        result.Id.ShouldBe(4);
        result.Name.ShouldBe("Append");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ByNameReturnsNotFoundForUnknownName()
    {
        var result = WriteModes.ByName("Unknown");

        result.ShouldNotBeNull();
        result.Name.ShouldBe("_Empty");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ByNameIsCaseSensitive()
    {
        WriteModes.ByName("Insert").ShouldNotBeNull();
        WriteModes.ByName("Insert").Name.ShouldBe("Insert");
        WriteModes.ByName("insert").Name.ShouldBe("_Empty");
        WriteModes.ByName("INSERT").Name.ShouldBe("_Empty");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void NotFoundReturnsEmptyInstance()
    {
        var result = WriteModes.NotFound;

        result.ShouldNotBeNull();
        result.Name.ShouldBe("_Empty");
        result.Id.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void AllContainsAllExpectedWriteModes()
    {
        var all = WriteModes.All();

        all.ShouldContain(m => m.Name == "Insert");
        all.ShouldContain(m => m.Name == "Upsert");
        all.ShouldContain(m => m.Name == "Replace");
        all.ShouldContain(m => m.Name == "Append");
    }
}
