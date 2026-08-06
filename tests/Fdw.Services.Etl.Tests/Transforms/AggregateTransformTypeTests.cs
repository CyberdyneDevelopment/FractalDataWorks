using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fdw.Configuration;
using Fdw.Services.Etl.Abstractions;
using Fdw.Services.Etl.Transforms;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Tests.Transforms;

/// <summary>
/// Tests for <see cref="AggregateTransformType"/> — the set-based group-by/reduce engine (FDW-556).
/// Covers N→M group-by correctness across single and multi-key grouping, every
/// <see cref="Fdw.Services.Etl.Abstractions.OptionTypes.AggregateFunctions"/> reducer
/// (Sum/Count/Avg/Min/Max/First/Last), the structural fail-loud gates (empty group-by/aggregations,
/// unknown aggregate function, wrong configuration type), the per-record <c>Transform</c> hard-fail
/// (aggregation is inherently set-based), and <c>MapSpecToConfiguration</c> request-spec dispatch.
/// </summary>
public sealed class AggregateTransformTypeTests
{
    private readonly AggregateTransformType _sut = new();

    private static TransformContext CreateContext() =>
        new(Guid.NewGuid(), NullLogger.Instance, new Dictionary<string, object?>());

    private static PipelineTransformConfiguration CreateConfig(
        IEnumerable<string> groupByFields,
        params PipelineTransformAggregationConfiguration[] aggregations)
    {
        var config = new PipelineTransformConfiguration { Id = Guid.NewGuid(), Name = "Agg1", OperationType = "Aggregate" };
        config.GroupByFields = groupByFields
            .Select((f, ordinal) => new PipelineTransformGroupByFieldConfiguration { Id = Guid.NewGuid(), FieldName = f, Ordinal = ordinal })
            .ToList();
        config.Aggregations = aggregations.ToList();
        return config;
    }

    private static PipelineTransformAggregationConfiguration Agg(string sourceField, string function, string outputField, int executionOrder = 0) =>
        new() { Id = Guid.NewGuid(), SourceField = sourceField, AggregateFunction = function, OutputField = outputField, ExecutionOrder = executionOrder };

    // ── Structural fail-loud branches ────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task TransformBatchFailsLoudWhenConfigurationIsNotPipelineTransformConfiguration()
    {
        // Arrange
        var configuration = Mock.Of<IGenericConfiguration>();
        var inputs = new List<IDictionary<string, object?>> { new Dictionary<string, object?>() };

        // Act
        var result = await _sut.TransformBatch(inputs, configuration, CreateContext(), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages[^1].Code.ShouldBe("ETL-11052");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task TransformBatchFailsLoudWhenGroupByFieldsIsEmpty()
    {
        // Arrange
        var config = CreateConfig([], Agg("Points", "Sum", "TotalPoints"));
        var inputs = new List<IDictionary<string, object?>> { new Dictionary<string, object?> { ["Points"] = 5 } };

        // Act
        var result = await _sut.TransformBatch(inputs, config, CreateContext(), TestContext.Current.CancellationToken);

        // Assert — a param-less Aggregate op fails loud, it never silently passes records through
        result.IsSuccess.ShouldBeFalse();
        result.Messages[^1].Code.ShouldBe("ETL-11045");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task TransformBatchFailsLoudWhenAggregationsIsEmpty()
    {
        // Arrange
        var config = CreateConfig(["TeamId"]);
        var inputs = new List<IDictionary<string, object?>> { new Dictionary<string, object?> { ["TeamId"] = "T1" } };

        // Act
        var result = await _sut.TransformBatch(inputs, config, CreateContext(), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages[^1].Code.ShouldBe("ETL-11045");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task TransformBatchFailsLoudWhenAggregateFunctionIsUnknown()
    {
        // Arrange
        var config = CreateConfig(["TeamId"], Agg("Points", "Median", "MedianPoints"));
        var inputs = new List<IDictionary<string, object?>> { new Dictionary<string, object?> { ["TeamId"] = "T1", ["Points"] = 5 } };

        // Act
        var result = await _sut.TransformBatch(inputs, config, CreateContext(), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages[^1].Code.ShouldBe("ETL-11050");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task TransformFailsLoudBecauseAggregationIsSetBasedNotPerRecord()
    {
        // Arrange — the per-record Transform entry point must never silently no-op; the engine must
        // call TransformBatch instead.
        var config = CreateConfig(["TeamId"], Agg("Points", "Sum", "TotalPoints"));
        var input = new Dictionary<string, object?> { ["TeamId"] = "T1", ["Points"] = 5 };

        // Act
        var result = await _sut.Transform(input, config, CreateContext(), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages[^1].Code.ShouldBe("ETL-11056");
    }

    // ── Group-by correctness: N → M dimensional reduction ───────────────────────────────

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task TransformBatchGroupsRecordsBySingleKeyAndSumsCorrectly()
    {
        // Arrange — 4 records collapse into 2 groups (TeamId A / TeamId B).
        var config = CreateConfig(["TeamId"], Agg("Points", "Sum", "TotalPoints"));
        var inputs = new List<IDictionary<string, object?>>
        {
            new Dictionary<string, object?> { ["TeamId"] = "A", ["Points"] = 10 },
            new Dictionary<string, object?> { ["TeamId"] = "A", ["Points"] = 20 },
            new Dictionary<string, object?> { ["TeamId"] = "B", ["Points"] = 5 },
            new Dictionary<string, object?> { ["TeamId"] = "B", ["Points"] = 15 },
        };

        // Act
        var result = await _sut.TransformBatch(inputs, config, CreateContext(), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var groups = new List<IDictionary<string, object?>>(result.Value!);
        groups.Count.ShouldBe(2);
        var teamA = groups.Single(g => Equals(g["TeamId"], "A"));
        var teamB = groups.Single(g => Equals(g["TeamId"], "B"));
        teamA["TotalPoints"].ShouldBe(30m);
        teamB["TotalPoints"].ShouldBe(20m);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task TransformBatchGroupsByMultipleKeysIndependently()
    {
        // Arrange — (TeamId, Season) composite key produces 3 distinct groups from 4 records.
        var config = CreateConfig(["TeamId", "Season"], Agg("Points", "Sum", "TotalPoints"));
        var inputs = new List<IDictionary<string, object?>>
        {
            new Dictionary<string, object?> { ["TeamId"] = "A", ["Season"] = 2024, ["Points"] = 10 },
            new Dictionary<string, object?> { ["TeamId"] = "A", ["Season"] = 2024, ["Points"] = 5 },
            new Dictionary<string, object?> { ["TeamId"] = "A", ["Season"] = 2025, ["Points"] = 7 },
            new Dictionary<string, object?> { ["TeamId"] = "B", ["Season"] = 2024, ["Points"] = 3 },
        };

        // Act
        var result = await _sut.TransformBatch(inputs, config, CreateContext(), TestContext.Current.CancellationToken);

        // Assert
        var groups = new List<IDictionary<string, object?>>(result.Value!);
        groups.Count.ShouldBe(3);
        groups.Single(g => Equals(g["TeamId"], "A") && Equals(g["Season"], 2024))["TotalPoints"].ShouldBe(15m);
        groups.Single(g => Equals(g["TeamId"], "A") && Equals(g["Season"], 2025))["TotalPoints"].ShouldBe(7m);
        groups.Single(g => Equals(g["TeamId"], "B") && Equals(g["Season"], 2024))["TotalPoints"].ShouldBe(3m);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task TransformBatchAppliesMultipleAggregationsInExecutionOrder()
    {
        // Arrange
        var config = CreateConfig(["TeamId"],
            Agg("Points", "Sum", "TotalPoints", executionOrder: 0),
            Agg("Points", "Count", "GameCount", executionOrder: 1),
            Agg("Points", "Avg", "AvgPoints", executionOrder: 2));
        var inputs = new List<IDictionary<string, object?>>
        {
            new Dictionary<string, object?> { ["TeamId"] = "A", ["Points"] = 10 },
            new Dictionary<string, object?> { ["TeamId"] = "A", ["Points"] = 20 },
        };

        // Act
        var result = await _sut.TransformBatch(inputs, config, CreateContext(), TestContext.Current.CancellationToken);

        // Assert
        var group = new List<IDictionary<string, object?>>(result.Value!)[0];
        group["TotalPoints"].ShouldBe(30m);
        group["GameCount"].ShouldBe(2);
        group["AvgPoints"].ShouldBe(15m);
    }

    // ── Every AggregateFunctions reducer ─────────────────────────────────────────────────

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    [InlineData("Sum", 60d)]
    [InlineData("Count", 3d)]
    [InlineData("Avg", 20d)]
    [InlineData("Min", 10d)]
    [InlineData("Max", 30d)]
    [InlineData("First", 10d)]
    [InlineData("Last", 30d)]
    public async Task TransformBatchAppliesEachAggregateFunctionCorrectly(string function, double expected)
    {
        // Arrange
        var config = CreateConfig(["TeamId"], Agg("Points", function, "Result"));
        var inputs = new List<IDictionary<string, object?>>
        {
            new Dictionary<string, object?> { ["TeamId"] = "A", ["Points"] = 10 },
            new Dictionary<string, object?> { ["TeamId"] = "A", ["Points"] = 20 },
            new Dictionary<string, object?> { ["TeamId"] = "A", ["Points"] = 30 },
        };

        // Act
        var result = await _sut.TransformBatch(inputs, config, CreateContext(), TestContext.Current.CancellationToken);

        // Assert — Sum/Avg/Count reduce to decimal/int; Min/Max/First/Last preserve the source CLR
        // type (int here), so compare via a culture-invariant numeric conversion rather than a cast.
        var group = new List<IDictionary<string, object?>>(result.Value!)[0];
        Convert.ToDecimal(group["Result"], System.Globalization.CultureInfo.InvariantCulture).ShouldBe((decimal)expected);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task TransformBatchOutputsNullForAggregationWhenAllSourceValuesAreNull()
    {
        // Arrange
        var config = CreateConfig(["TeamId"], Agg("Points", "Sum", "TotalPoints"));
        var inputs = new List<IDictionary<string, object?>>
        {
            new Dictionary<string, object?> { ["TeamId"] = "A", ["Points"] = null },
        };

        // Act
        var result = await _sut.TransformBatch(inputs, config, CreateContext(), TestContext.Current.CancellationToken);

        // Assert
        var group = new List<IDictionary<string, object?>>(result.Value!)[0];
        group["TotalPoints"].ShouldBeNull();
    }

    // ── MapSpecToConfiguration: request-spec → typed config dispatch (FDW-556 Part 2.2) ─

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void MapSpecToConfigurationPopulatesGroupByFieldsAndAggregations()
    {
        // Arrange
        var spec = new FakeTransformOperationSpec
        {
            Name = "Agg1",
            OperationType = "Aggregate",
            GroupByFields = ["TeamId"],
            Aggregations = [new FakeAggregationSpec { SourceField = "Points", Function = "Sum", OutputField = "TotalPoints" }],
        };
        var target = new PipelineTransformConfiguration { Id = Guid.NewGuid(), Name = "Agg1", OperationType = "Aggregate" };

        // Act
        var result = _sut.MapSpecToConfiguration(spec, target, NullLogger.Instance);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        target.GroupByFields.Count.ShouldBe(1);
        target.GroupByFields[0].FieldName.ShouldBe("TeamId");
        target.Aggregations.Count.ShouldBe(1);
        target.Aggregations[0].SourceField.ShouldBe("Points");
        target.Aggregations[0].AggregateFunction.ShouldBe("Sum");
        target.Aggregations[0].OutputField.ShouldBe("TotalPoints");
        target.Aggregations[0].PipelineTransformId.ShouldBe(target.Id);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void MapSpecToConfigurationFailsLoudWhenGroupByFieldsEmpty()
    {
        // Arrange
        var spec = new FakeTransformOperationSpec
        {
            Name = "Agg1",
            OperationType = "Aggregate",
            Aggregations = [new FakeAggregationSpec { SourceField = "Points", Function = "Sum", OutputField = "TotalPoints" }],
        };
        var target = new PipelineTransformConfiguration { Id = Guid.NewGuid(), Name = "Agg1", OperationType = "Aggregate" };

        // Act
        var result = _sut.MapSpecToConfiguration(spec, target, NullLogger.Instance);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages[^1].Code.ShouldBe("ETL-11045");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void MapSpecToConfigurationFailsLoudWhenAggregateFunctionUnknown()
    {
        // Arrange
        var spec = new FakeTransformOperationSpec
        {
            Name = "Agg1",
            OperationType = "Aggregate",
            GroupByFields = ["TeamId"],
            Aggregations = [new FakeAggregationSpec { SourceField = "Points", Function = "Median", OutputField = "MedianPoints" }],
        };
        var target = new PipelineTransformConfiguration { Id = Guid.NewGuid(), Name = "Agg1", OperationType = "Aggregate" };

        // Act
        var result = _sut.MapSpecToConfiguration(spec, target, NullLogger.Instance);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages[^1].Code.ShouldBe("ETL-11050");
    }
}
