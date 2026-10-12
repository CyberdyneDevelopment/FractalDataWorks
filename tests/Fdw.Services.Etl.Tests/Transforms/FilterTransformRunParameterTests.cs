using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fdw.Services.Etl.Transforms;
using Microsoft.Extensions.Logging.Abstractions;
using Fdw.Services.Etl.Abstractions;

namespace Fdw.Services.Etl.Tests.Transforms;

/// <summary>
/// Tests for run parameters in <see cref="FilterTransformType"/> (FDW-1109): <c>{{name}}</c> in the filter expression reads
/// the run's parameters, and a point in time compares as a point in time so the watermark filters correctly.
/// </summary>
public sealed class FilterTransformRunParameterTests
{
    private readonly FilterTransformType _sut = new();

    private static TransformContext Context(params (string Name, object? Value)[] parameters)
    {
        var values = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var (name, value) in parameters)
            values[name] = value;
        return new TransformContext(Guid.NewGuid(), NullLogger.Instance, new Dictionary<string, object?>(), fieldTransformChains: new Dictionary<Guid, IReadOnlyList<IFieldMappingTransform>>(), parameters: values);
    }

    private static PipelineTransformConfiguration Filter(string expression) =>
        new() { Id = Guid.NewGuid(), Name = "Filter1", OperationType = "Filter", FilterExpression = expression };

    private static Dictionary<string, object?> Row(string label, params (string Field, object? Value)[] fields)
    {
        var row = new Dictionary<string, object?> { ["Label"] = label };
        foreach (var (field, value) in fields)
            row[field] = value;
        return row;
    }

    private static string[] Labels(IEnumerable<IDictionary<string, object?>> rows) => rows.Select(r => (string)r["Label"]!).ToArray();

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ARunParameterFiltersTheExtract()
    {
        // Arrange
        var inputs = new List<IDictionary<string, object?>> { Row("low", ("Points", 1)), Row("edge", ("Points", 2)), Row("high", ("Points", 9)) };

        // Act
        var result = await _sut.TransformBatch(inputs, Filter("Points >= {{MinPoints}}"), Context(("MinPoints", 2L)), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        Labels(result.Value!).ShouldBe(new[] { "edge", "high" });
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ATextParameterFiltersByEquality()
    {
        // Arrange
        var inputs = new List<IDictionary<string, object?>> { Row("a", ("Region", "EU")), Row("b", ("Region", "US")), Row("c", ("Region", "eu")) };

        // Act
        var result = await _sut.TransformBatch(inputs, Filter("Region == {{Region}}"), Context(("Region", "EU")), TestContext.Current.CancellationToken);

        // Assert
        Labels(result.Value!).ShouldBe(new[] { "a", "c" });
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task TheWatermarkFiltersByPointInTimeNotByText()
    {
        // Arrange: the watermark is 2026-03-01 UTC; a text comparison of the default date strings would order these wrongly
        var watermark = new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero);
        var inputs = new List<IDictionary<string, object?>>
        {
            Row("before", ("Modified", new DateTimeOffset(2026, 2, 28, 23, 59, 59, TimeSpan.Zero))),
            Row("after", ("Modified", new DateTimeOffset(2026, 3, 1, 0, 0, 1, TimeSpan.Zero))),
            Row("sameInstantOtherOffset", ("Modified", new DateTimeOffset(2026, 3, 1, 2, 0, 0, TimeSpan.FromHours(2)))),
            Row("later", ("Modified", new DateTimeOffset(2026, 12, 25, 0, 0, 0, TimeSpan.Zero))),
            Row("earlierOtherOffset", ("Modified", new DateTimeOffset(2026, 3, 1, 0, 30, 0, TimeSpan.FromHours(2)))),
        };

        // Act
        var result = await _sut.TransformBatch(
            inputs, Filter("Modified > {{LastSuccessfulRun}}"), Context(("LastSuccessfulRun", watermark)), TestContext.Current.CancellationToken);

        // Assert: 00:30+02:00 is 22:30 the day before, so it is not after the watermark; 02:00+02:00 is exactly the watermark
        Labels(result.Value!).ShouldBe(new[] { "after", "later" });
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ADateTimeWithNoKindIsReadAsUtcWhenComparedToAPointInTime()
    {
        // Arrange
        var watermark = new DateTimeOffset(2026, 3, 1, 12, 0, 0, TimeSpan.Zero);
        var inputs = new List<IDictionary<string, object?>>
        {
            Row("older", ("Modified", new DateTime(2026, 3, 1, 11, 0, 0, DateTimeKind.Unspecified))),
            Row("newer", ("Modified", new DateTime(2026, 3, 1, 13, 0, 0, DateTimeKind.Unspecified))),
        };

        // Act
        var result = await _sut.TransformBatch(
            inputs, Filter("Modified >= {{LastSuccessfulRun}}"), Context(("LastSuccessfulRun", watermark)), TestContext.Current.CancellationToken);

        // Assert
        Labels(result.Value!).ShouldBe(new[] { "newer" });
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AReferenceToAParameterTheRunLacksFailsTheStepAndNamesIt()
    {
        // Act
        var result = await _sut.TransformBatch(
            [Row("a", ("Points", 1))], Filter("Points >= {{MinPoints}}"), Context(), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages[^1].Code.ShouldBe("PIPELINERUN-20205");
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("MinPoints");
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("transform 'Filter1'");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ARunWithNoWatermarkFailsAFilterThatReferencesIt()
    {
        // Act: a first run, nothing stored and nothing overriding it
        var result = await _sut.TransformBatch(
            [Row("a", ("Modified", DateTimeOffset.UtcNow))], Filter("Modified > {{LastSuccessfulRun}}"), Context(), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages[^1].Code.ShouldBe("PIPELINERUN-20206");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ATextParameterThatCouldChangeTheExpressionIsRefused()
    {
        // Act
        var result = await _sut.TransformBatch(
            [Row("a", ("Region", "EU"))], Filter("Region == {{Region}}"), Context(("Region", "x || Points > 0")), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages[^1].Code.ShouldBe("PIPELINERUN-20207");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task TransformResolvesReferencesForASingleRecordToo()
    {
        // Arrange
        var context = Context(("MinPoints", 5L));

        // Act
        var passes = await _sut.Transform(Row("a", ("Points", 9)), Filter("Points >= {{MinPoints}}"), context, TestContext.Current.CancellationToken);
        var filtered = await _sut.Transform(Row("b", ("Points", 1)), Filter("Points >= {{MinPoints}}"), context, TestContext.Current.CancellationToken);
        var refused = await _sut.Transform(Row("c", ("Points", 9)), Filter("Points >= {{Missing}}"), context, TestContext.Current.CancellationToken);

        // Assert
        passes.Value.ShouldNotBeNull();
        filtered.Value.ShouldBeNull();
        refused.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task AFilterWithoutReferencesBehavesAsItAlwaysDid()
    {
        // Arrange
        var inputs = new List<IDictionary<string, object?>> { Row("a", ("Points", 1)), Row("b", ("Points", 9)) };

        // Act
        var result = await _sut.TransformBatch(inputs, Filter("Points > 5"), Context(), TestContext.Current.CancellationToken);

        // Assert
        Labels(result.Value!).ShouldBe(new[] { "b" });
    }
}
