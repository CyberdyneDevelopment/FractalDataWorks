using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fdw.Services.Etl.Transforms;
using Microsoft.Extensions.Logging.Abstractions;
using Fdw.Services.Etl.Abstractions;

namespace Fdw.Services.Etl.Tests.Transforms;

/// <summary>
/// Tests for <see cref="DistinctTransformType"/> (FDW-1109): the first row of each key survives, in input order.
/// </summary>
public sealed class DistinctTransformTypeTests
{
    private readonly DistinctTransformType _sut = new();

    private static TransformContext CreateContext() =>
        new(Guid.NewGuid(), NullLogger.Instance, new Dictionary<string, object?>(), fieldTransformChains: new Dictionary<Guid, IReadOnlyList<IFieldMappingTransform>>());

    private static PipelineTransformConfiguration CreateConfig(params string[] keys)
    {
        var config = new PipelineTransformConfiguration { Id = Guid.NewGuid(), Name = "Unique", OperationType = "Distinct" };
        config.GroupByFields = keys
            .Select((key, ordinal) => new PipelineTransformGroupByFieldConfiguration { Id = Guid.NewGuid(), Name = key, FieldName = key, Ordinal = ordinal })
            .ToList();
        return config;
    }

    private static Dictionary<string, object?> Row(object? key, string label) =>
        new Dictionary<string, object?> { ["Key"] = key, ["Label"] = label };

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void RequiresCompleteInputBecauseWhichRowSurvivesIsTheInputOrdersCall()
    {
        _sut.RequiresCompleteInput.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task TransformBatchKeepsTheFirstRowOfEachKeyInInputOrder()
    {
        // Arrange
        var inputs = new List<IDictionary<string, object?>> { Row("a", "a1"), Row("b", "b1"), Row("A", "a2"), Row("c", "c1"), Row("b", "b2") };

        // Act
        var result = await _sut.TransformBatch(inputs, CreateConfig("Key"), CreateContext(), TestContext.Current.CancellationToken);

        // Assert: keys compare ignoring case, as the Aggregate transform groups
        result.IsSuccess.ShouldBeTrue();
        result.Value!.Select(r => r["Label"]).ShouldBe(new object?[] { "a1", "b1", "c1" });
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task TransformBatchComparesTheWholeKeyNotItsParts()
    {
        // Arrange: ("a","bc") and ("ab","c") are different keys even though they join to the same text
        var config = CreateConfig("Key", "Label");
        var inputs = new List<IDictionary<string, object?>> { Row("a", "bc"), Row("ab", "c"), Row("a", "bc") };

        // Act
        var result = await _sut.TransformBatch(inputs, config, CreateContext(), TestContext.Current.CancellationToken);

        // Assert
        result.Value!.Count().ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task TransformBatchKeepsOneRowForANullKeyAndTreatsItAsDifferentFromEmptyText()
    {
        // Arrange
        var inputs = new List<IDictionary<string, object?>> { Row(null, "n1"), Row(null, "n2"), Row(string.Empty, "empty") };

        // Act
        var result = await _sut.TransformBatch(inputs, CreateConfig("Key"), CreateContext(), TestContext.Current.CancellationToken);

        // Assert
        result.Value!.Select(r => r["Label"]).ShouldBe(new object?[] { "n1", "empty" });
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task TransformBatchFailsLoudWhenThereAreNoKeyFields()
    {
        // Act
        var result = await _sut.TransformBatch([Row("a", "a1")], CreateConfig(), CreateContext(), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages[^1].Code.ShouldBe("TRANSFORM-20101");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task TransformBatchFailsLoudWhenARecordLacksAKeyField()
    {
        // Act
        var result = await _sut.TransformBatch([Row("a", "a1")], CreateConfig("Missing"), CreateContext(), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages[^1].Code.ShouldBe("TRANSFORM-20102");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void MapSpecToConfigurationWritesTheKeyFields()
    {
        // Arrange
        var spec = new FakeTransformOperationSpec { Name = "Unique", OperationType = "Distinct", GroupByFields = ["Key"] };
        var config = new PipelineTransformConfiguration();

        // Act
        var result = _sut.MapSpecToConfiguration(spec, config, NullLogger.Instance);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        config.GroupByFields.Select(f => f.FieldName).ShouldBe(new[] { "Key" });
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void MapSpecToConfigurationRefusesASpecWithNoKeyFields()
    {
        // Act
        var result = _sut.MapSpecToConfiguration(
            new FakeTransformOperationSpec { Name = "Unique", OperationType = "Distinct" }, new PipelineTransformConfiguration(), NullLogger.Instance);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages[^1].Code.ShouldBe("TRANSFORM-20101");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task TransformFailsBecauseADistinctNeedsTheWholeSet()
    {
        // Act
        var result = await _sut.Transform(new Dictionary<string, object?>(), CreateConfig("Key"), CreateContext(), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages[^1].Code.ShouldBe("ETL-11056");
    }
}
