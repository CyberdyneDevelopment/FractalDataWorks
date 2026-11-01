using Fdw.Services.Etl.Pipelines.Loads;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Tests.Pipelines.Loads;

/// <summary>
/// Tests for <see cref="LoadRows"/> (FDW-1109): rows are shaped by copy for each load, so no load changes the rows another
/// reads.
/// </summary>
public sealed class LoadRowsTests
{
    private static readonly IReadOnlyList<KeyValuePair<string, object?>> NoValues = [];

    private static List<IDictionary<string, object?>> Rows() =>
    [
        new Dictionary<string, object?> { ["Id"] = 1, ["Name"] = "a", ["Extra"] = "x" },
        new Dictionary<string, object?> { ["Id"] = 2, ["Name"] = "b", ["Extra"] = "y" },
    ];

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void ShapeRestrictsEachRowToTheNamedColumns()
    {
        // Act
        var result = LoadRows.Shape("test", Rows(), ["Id", "Name"], NoValues, NullLogger.Instance);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var shaped = result.Value!;
        shaped.Select(r => string.Join(",", r.Keys.OrderBy(k => k, StringComparer.Ordinal))).ShouldAllBe(keys => keys == "Id,Name");
        shaped[1]["Name"].ShouldBe("b");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void ShapeMatchesAColumnToARowFieldIgnoringCase()
    {
        // Act
        var result = LoadRows.Shape("test", Rows(), ["id"], NoValues, NullLogger.Instance);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value![0]["id"].ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void ShapeAddsTheValuesTheValueSourcesGive()
    {
        // Arrange
        IReadOnlyList<KeyValuePair<string, object?>> values = [new("Batch", 7L), new("LoadedBy", "etl")];

        // Act
        var result = LoadRows.Shape("test", Rows(), ["Id"], values, NullLogger.Instance);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value![0]["Batch"].ShouldBe(7L);
        result.Value[1]["LoadedBy"].ShouldBe("etl");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void ShapeKeepsEveryFieldWhenNoColumnsAreNamed()
    {
        // Arrange
        var rows = Rows();

        // Act
        var result = LoadRows.Shape("test", rows, null, NoValues, NullLogger.Instance);

        // Assert: nothing to restrict or add, so the rows themselves go on
        result.IsSuccess.ShouldBeTrue();
        result.Value![0].ShouldBeSameAs(rows[0]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void ShapeDoesNotChangeTheRowsItIsGiven()
    {
        // Arrange
        var rows = Rows();
        IReadOnlyList<KeyValuePair<string, object?>> values = [new("Batch", 7L)];

        // Act
        LoadRows.Shape("test", rows, ["Id"], values, NullLogger.Instance);
        LoadRows.Shape("test", rows, null, values, NullLogger.Instance);

        // Assert
        rows[0].Keys.OrderBy(k => k, StringComparer.Ordinal).ShouldBe(new[] { "Extra", "Id", "Name" });
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ShapeFailsLoudWhenARowLacksANamedColumn()
    {
        // Act
        var result = LoadRows.Shape("load step 'x'", Rows(), ["Id", "Missing"], NoValues, NullLogger.Instance);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages[^1].Code.ShouldBe("PIPELINERUN-20214");
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("Missing");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ShapeFailsLoudWhenAColumnIsNamedAndSuppliedByAValueSource()
    {
        // Arrange
        IReadOnlyList<KeyValuePair<string, object?>> values = [new("name", "z")];

        // Act
        var result = LoadRows.Shape("test", Rows(), ["Id", "Name"], values, NullLogger.Instance);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages[^1].Code.ShouldBe("PIPELINERUN-20215");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ShapeFailsLoudWhenARowAlreadyCarriesAFieldAValueSourceSupplies()
    {
        // Arrange: no columns named, so every field travels, and one is also supplied
        IReadOnlyList<KeyValuePair<string, object?>> values = [new("Name", "z")];

        // Act
        var result = LoadRows.Shape("test", Rows(), null, values, NullLogger.Instance);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages[^1].Code.ShouldBe("PIPELINERUN-20215");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ShapeOfNoRowsIsNoRows()
    {
        // Act
        var result = LoadRows.Shape("test", [], ["Id"], NoValues, NullLogger.Instance);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value!.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void TryFindMatchesTheFieldIgnoringCaseAndReportsNullValues()
    {
        // Arrange
        IDictionary<string, object?> row = new Dictionary<string, object?> { ["Code"] = null };

        // Act
        var found = LoadRows.TryFind(row, "CODE", out var value);
        var missing = LoadRows.TryFind(row, "Other", out _);

        // Assert
        found.ShouldBeTrue();
        value.ShouldBeNull();
        missing.ShouldBeFalse();
    }
}
