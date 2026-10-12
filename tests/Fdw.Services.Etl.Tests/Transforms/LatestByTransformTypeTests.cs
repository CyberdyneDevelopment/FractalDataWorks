using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fdw.Services.Etl.Abstractions;
using Fdw.Services.Etl.Transforms;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Tests.Transforms;

/// <summary>
/// Tests for <see cref="LatestByTransformType"/> (FDW-1109): one row per key, the first or the last by the order field,
/// ties, composite and case-insensitive keys, and the refusals of a step that cannot run.
/// </summary>
public sealed class LatestByTransformTypeTests
{
    private readonly LatestByTransformType _sut = new();

    private static TransformContext CreateContext() =>
        new(Guid.NewGuid(), NullLogger.Instance, new Dictionary<string, object?>(), fieldTransformChains: new Dictionary<Guid, IReadOnlyList<IFieldMappingTransform>>());

    private static PipelineTransformConfiguration CreateConfig(string[] keys, string? orderField, string? keepRule)
    {
        var config = new PipelineTransformConfiguration { Id = Guid.NewGuid(), Name = "Latest", OperationType = "LatestBy" };
        config.GroupByFields = keys
            .Select((key, ordinal) => new PipelineTransformGroupByFieldConfiguration { Id = Guid.NewGuid(), Name = key, FieldName = key, Ordinal = ordinal })
            .ToList();
        config.OrderField = orderField;
        config.KeepRule = keepRule;
        return config;
    }

    private static Dictionary<string, object?> Row(object? key, object? version, string label) =>
        new Dictionary<string, object?> { ["Key"] = key, ["Version"] = version, ["Label"] = label };

    private static string[] Labels(IEnumerable<IDictionary<string, object?>> rows) =>
        rows.Select(r => (string)r["Label"]!).ToArray();

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void RequiresCompleteInputBecauseTheWinnerOfAKeyNeedsEveryRowOfIt()
    {
        _sut.RequiresCompleteInput.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task TransformBatchKeepsTheRowWithTheLargestOrderValueForLast()
    {
        // Arrange
        var config = CreateConfig(["Key"], "Version", "Last");
        var inputs = new List<IDictionary<string, object?>>
        {
            Row("a", 1, "a1"), Row("b", 5, "b5"), Row("a", 3, "a3"), Row("b", 2, "b2"), Row("a", 2, "a2"),
        };

        // Act
        var result = await _sut.TransformBatch(inputs, config, CreateContext(), TestContext.Current.CancellationToken);

        // Assert: one row per key, in the order the keys first appeared
        result.IsSuccess.ShouldBeTrue();
        Labels(result.Value!).ShouldBe(new[] { "a3", "b5" });
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task TransformBatchKeepsTheRowWithTheSmallestOrderValueForFirst()
    {
        // Arrange
        var config = CreateConfig(["Key"], "Version", "First");
        var inputs = new List<IDictionary<string, object?>>
        {
            Row("a", 3, "a3"), Row("a", 1, "a1"), Row("b", 2, "b2"), Row("b", 4, "b4"),
        };

        // Act
        var result = await _sut.TransformBatch(inputs, config, CreateContext(), TestContext.Current.CancellationToken);

        // Assert
        Labels(result.Value!).ShouldBe(new[] { "a1", "b2" });
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task TransformBatchBreaksATieByInputOrderFirstKeepsTheEarlierLastTheLater()
    {
        // Arrange
        var inputs = new List<IDictionary<string, object?>> { Row("a", 1, "early"), Row("a", 1, "late") };

        // Act
        var first = await _sut.TransformBatch(inputs, CreateConfig(["Key"], "Version", "First"), CreateContext(), TestContext.Current.CancellationToken);
        var last = await _sut.TransformBatch(inputs, CreateConfig(["Key"], "Version", "Last"), CreateContext(), TestContext.Current.CancellationToken);

        // Assert
        Labels(first.Value!).ShouldBe(new[] { "early" });
        Labels(last.Value!).ShouldBe(new[] { "late" });
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task TransformBatchTreatsKeysIgnoringCaseAndNumbersByValue()
    {
        // Arrange
        var config = CreateConfig(["Key"], "Version", "Last");
        var inputs = new List<IDictionary<string, object?>>
        {
            Row("Abc", 1, "upper"), Row("aBC", 2, "mixed"), Row(7, 1, "int"), Row(7L, 2, "long"),
        };

        // Act
        var result = await _sut.TransformBatch(inputs, config, CreateContext(), TestContext.Current.CancellationToken);

        // Assert
        Labels(result.Value!).ShouldBe(new[] { "mixed", "long" });
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task TransformBatchGroupsOnEveryKeyFieldTogether()
    {
        // Arrange
        var config = CreateConfig(["Key", "Label"], "Version", "Last");
        var inputs = new List<IDictionary<string, object?>>
        {
            Row("a", 1, "x"), Row("a", 2, "y"), Row("a", 3, "x"),
        };

        // Act
        var result = await _sut.TransformBatch(inputs, config, CreateContext(), TestContext.Current.CancellationToken);

        // Assert: (a,x) keeps version 3, (a,y) keeps its only row
        result.Value!.Select(r => (r["Label"], r["Version"])).ShouldBe(new[] { ((object?)"x", (object?)3), ("y", 2) });
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task TransformBatchTreatsANullKeyAsOneGroup()
    {
        // Arrange
        var config = CreateConfig(["Key"], "Version", "Last");
        var inputs = new List<IDictionary<string, object?>> { Row(null, 1, "n1"), Row(null, 2, "n2"), Row("a", 1, "a1") };

        // Act
        var result = await _sut.TransformBatch(inputs, config, CreateContext(), TestContext.Current.CancellationToken);

        // Assert
        Labels(result.Value!).ShouldBe(new[] { "n2", "a1" });
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task TransformBatchOfNoRowsIsEmpty()
    {
        // Act
        var result = await _sut.TransformBatch([], CreateConfig(["Key"], "Version", "Last"), CreateContext(), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value!.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task TransformBatchFailsLoudWhenOrderValuesHaveNoCommonOrder()
    {
        // Arrange
        var inputs = new List<IDictionary<string, object?>> { Row("a", 1, "a1"), Row("a", "text", "a2") };

        // Act
        var result = await _sut.TransformBatch(inputs, CreateConfig(["Key"], "Version", "Last"), CreateContext(), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages[^1].Code.ShouldBe("TRANSFORM-20103");
    }

    [Theory]
    [InlineData("Key", "Missing")]
    [InlineData("Missing", "Version")]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task TransformBatchFailsLoudWhenARecordLacksTheKeyOrTheOrderField(string key, string orderField)
    {
        // Arrange
        var inputs = new List<IDictionary<string, object?>> { Row("a", 1, "a1") };

        // Act
        var result = await _sut.TransformBatch(inputs, CreateConfig([key], orderField, "Last"), CreateContext(), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages[^1].Code.ShouldBe("TRANSFORM-20102");
    }

    [Theory]
    [InlineData(null, "Version", "Last")]
    [InlineData("Key", null, "Last")]
    [InlineData("Key", "", "Last")]
    [InlineData("Key", "Version", null)]
    [InlineData("Key", "Version", "Newest")]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task TransformBatchFailsLoudWhenTheKeyOrderFieldOrKeepRuleIsMissingOrUnknown(string? key, string? orderField, string? keepRule)
    {
        // Arrange
        string[] keys = key is null ? [] : [key];
        var inputs = new List<IDictionary<string, object?>> { Row("a", 1, "a1") };

        // Act
        var result = await _sut.TransformBatch(inputs, CreateConfig(keys, orderField, keepRule), CreateContext(), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages[^1].Code.ShouldBe("TRANSFORM-20101");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task TransformFailsBecauseALatestByNeedsTheWholeSet()
    {
        // Act
        var result = await _sut.Transform(new Dictionary<string, object?>(), CreateConfig(["Key"], "Version", "Last"), CreateContext(), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages[^1].Code.ShouldBe("ETL-11056");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void MapSpecToConfigurationWritesKeysOrderFieldAndKeepRule()
    {
        // Arrange
        var spec = new FakeTransformOperationSpec
        {
            Name = "Latest",
            OperationType = "LatestBy",
            GroupByFields = ["Key", "Label"],
            OrderField = "Version",
            KeepRule = "Last",
        };
        var config = new PipelineTransformConfiguration();

        // Act
        var result = _sut.MapSpecToConfiguration(spec, config, NullLogger.Instance);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        config.GroupByFields.OrderBy(f => f.Ordinal).Select(f => f.FieldName).ShouldBe(new[] { "Key", "Label" });
        config.OrderField.ShouldBe("Version");
        config.KeepRule.ShouldBe("Last");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void MapSpecToConfigurationRefusesASpecWithoutAnOrderField()
    {
        // Arrange
        var spec = new FakeTransformOperationSpec { Name = "Latest", OperationType = "LatestBy", GroupByFields = ["Key"], KeepRule = "Last" };

        // Act
        var result = _sut.MapSpecToConfiguration(spec, new PipelineTransformConfiguration(), NullLogger.Instance);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages[^1].Code.ShouldBe("TRANSFORM-20101");
    }
}
