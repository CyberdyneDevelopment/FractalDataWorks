using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fdw.Services.Etl.Abstractions;
using Fdw.Services.Etl.Transforms;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Tests.Transforms;

/// <summary>
/// Tests for <see cref="SortTransformType"/> (FDW-1109): the ordering of the output, its stability, how it treats nulls and
/// values with no common order, and the refusals of a step that cannot run.
/// </summary>
public sealed class SortTransformTypeTests
{
    private readonly SortTransformType _sut = new();

    private static TransformContext CreateContext() =>
        new(Guid.NewGuid(), NullLogger.Instance, new Dictionary<string, object?>(), fieldTransformChains: new Dictionary<Guid, IReadOnlyList<IFieldMappingTransform>>());

    private static PipelineTransformConfiguration CreateConfig(params (string Field, string Direction)[] fields)
    {
        var config = new PipelineTransformConfiguration { Id = Guid.NewGuid(), Name = "Order", OperationType = "Sort" };
        config.SortFields = fields
            .Select((f, ordinal) => new PipelineTransformSortFieldConfiguration
            {
                Id = Guid.NewGuid(),
                Name = f.Field,
                FieldName = f.Field,
                Direction = f.Direction,
                Ordinal = ordinal,
            })
            .ToList();
        return config;
    }

    private static List<IDictionary<string, object?>> Rows(params (object? Team, object? Points)[] rows) =>
        rows.Select(r => (IDictionary<string, object?>)new Dictionary<string, object?> { ["Team"] = r.Team, ["Points"] = r.Points }).ToList();

    private static string[] Teams(IEnumerable<IDictionary<string, object?>> rows) =>
        rows.Select(r => (string)r["Team"]!).ToArray();

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void RequiresCompleteInputBecauseAnOrderingIsAPropertyOfTheWholeSet()
    {
        _sut.RequiresCompleteInput.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task TransformBatchOrdersAscendingByTheField()
    {
        // Arrange
        var config = CreateConfig(("Points", "Ascending"));
        var inputs = Rows(("c", 3), ("a", 1), ("b", 2));

        // Act
        var result = await _sut.TransformBatch(inputs, config, CreateContext(), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        Teams(result.Value!).ShouldBe(new[] { "a", "b", "c" });
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task TransformBatchOrdersDescendingByTheField()
    {
        // Arrange
        var config = CreateConfig(("Points", "Descending"));
        var inputs = Rows(("a", 1), ("c", 3), ("b", 2));

        // Act
        var result = await _sut.TransformBatch(inputs, config, CreateContext(), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        Teams(result.Value!).ShouldBe(new[] { "c", "b", "a" });
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task TransformBatchBreaksTiesOnTheNextFieldInItsOwnDirection()
    {
        // Arrange: points high to low, and within equal points the team name low to high
        var config = CreateConfig(("Points", "Descending"), ("Team", "Ascending"));
        var inputs = Rows(("b", 5), ("a", 5), ("c", 9), ("d", 1));

        // Act
        var result = await _sut.TransformBatch(inputs, config, CreateContext(), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        Teams(result.Value!).ShouldBe(new[] { "c", "a", "b", "d" });
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task TransformBatchKeepsInputOrderForRowsEqualOnEverySortField()
    {
        // Arrange
        var config = CreateConfig(("Points", "Ascending"));
        var inputs = Rows(("first", 1), ("second", 1), ("third", 1));

        // Act
        var result = await _sut.TransformBatch(inputs, config, CreateContext(), TestContext.Current.CancellationToken);

        // Assert
        Teams(result.Value!).ShouldBe(new[] { "first", "second", "third" });
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task TransformBatchSortsNullsFirstInAscendingOrderAndLastInDescending()
    {
        // Arrange
        var ascending = CreateConfig(("Points", "Ascending"));
        var descending = CreateConfig(("Points", "Descending"));
        var inputs = Rows(("a", 2), ("n", null), ("b", 1));

        // Act
        var up = await _sut.TransformBatch(inputs, ascending, CreateContext(), TestContext.Current.CancellationToken);
        var down = await _sut.TransformBatch(inputs, descending, CreateContext(), TestContext.Current.CancellationToken);

        // Assert
        Teams(up.Value!).ShouldBe(new[] { "n", "b", "a" });
        Teams(down.Value!).ShouldBe(new[] { "a", "b", "n" });
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task TransformBatchComparesNumbersOfDifferentTypesByValue()
    {
        // Arrange
        var config = CreateConfig(("Points", "Ascending"));
        var inputs = Rows(("long", 10L), ("int", 9), ("decimal", 9.5m), ("double", 10.5d));

        // Act
        var result = await _sut.TransformBatch(inputs, config, CreateContext(), TestContext.Current.CancellationToken);

        // Assert
        Teams(result.Value!).ShouldBe(new[] { "int", "decimal", "long", "double" });
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task TransformBatchFailsLoudWhenValuesOfTheFieldHaveNoCommonOrder()
    {
        // Arrange: a number against text
        var config = CreateConfig(("Points", "Ascending"));
        var inputs = Rows(("a", 1), ("b", "text"));

        // Act
        var result = await _sut.TransformBatch(inputs, config, CreateContext(), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages[^1].Code.ShouldBe("TRANSFORM-20103");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task TransformBatchFailsLoudWhenARecordLacksTheSortField()
    {
        // Arrange
        var config = CreateConfig(("Missing", "Ascending"));
        var inputs = Rows(("a", 1));

        // Act
        var result = await _sut.TransformBatch(inputs, config, CreateContext(), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages[^1].Code.ShouldBe("TRANSFORM-20102");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task TransformBatchFailsLoudWhenThereAreNoSortFields()
    {
        // Act
        var result = await _sut.TransformBatch(Rows(("a", 1)), CreateConfig(), CreateContext(), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages[^1].Code.ShouldBe("TRANSFORM-20101");
    }

    [Theory]
    [InlineData("Sideways")]
    [InlineData("")]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task TransformBatchFailsLoudWhenTheDirectionIsNotAscendingOrDescending(string direction)
    {
        // Act
        var result = await _sut.TransformBatch(Rows(("a", 1)), CreateConfig(("Points", direction)), CreateContext(), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages[^1].Code.ShouldBe("TRANSFORM-20101");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task TransformBatchFailsLoudWhenAFieldIsSortedTwice()
    {
        // Act
        var result = await _sut.TransformBatch(
            Rows(("a", 1)), CreateConfig(("Points", "Ascending"), ("points", "Descending")), CreateContext(), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages[^1].Code.ShouldBe("TRANSFORM-20101");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task TransformBatchFailsLoudWhenConfigurationIsNotPipelineTransformConfiguration()
    {
        // Arrange
        var configuration = Mock.Of<Fdw.Configuration.IGenericConfiguration>();

        // Act
        var result = await _sut.TransformBatch(Rows(("a", 1)), configuration, CreateContext(), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages[^1].Code.ShouldBe("ETL-11052");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task TransformFailsBecauseAnOrderingNeedsTheWholeSet()
    {
        // Act
        var result = await _sut.Transform(new Dictionary<string, object?>(), CreateConfig(("Points", "Ascending")), CreateContext(), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages[^1].Code.ShouldBe("ETL-11056");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void MapSpecToConfigurationWritesTheSortFieldsInOrder()
    {
        // Arrange
        var spec = new FakeTransformOperationSpec
        {
            Name = "Order",
            OperationType = "Sort",
            SortFields = [new FakeSortFieldSpec("Points", "Descending"), new FakeSortFieldSpec("Team", "Ascending")],
        };
        var config = new PipelineTransformConfiguration();

        // Act
        var result = _sut.MapSpecToConfiguration(spec, config, NullLogger.Instance);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        config.SortFields.Select(f => (f.FieldName, f.Direction, f.Ordinal))
            .ShouldBe(new[] { ("Points", "Descending", 0), ("Team", "Ascending", 1) });
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void MapSpecToConfigurationRefusesASpecWithNoSortFields()
    {
        // Act
        var result = _sut.MapSpecToConfiguration(
            new FakeTransformOperationSpec { Name = "Order", OperationType = "Sort" }, new PipelineTransformConfiguration(), NullLogger.Instance);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages[^1].Code.ShouldBe("TRANSFORM-20101");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void MapSpecToConfigurationRefusesARequestThatCannotCarrySortFields()
    {
        // Arrange: a spec that does not implement the ordering interface
        var spec = new Mock<ITransformOperationSpec>();
        spec.SetupGet(s => s.Name).Returns("Order");
        spec.SetupGet(s => s.OperationType).Returns("Sort");

        // Act
        var result = _sut.MapSpecToConfiguration(spec.Object, new PipelineTransformConfiguration(), NullLogger.Instance);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages[^1].Code.ShouldBe("TRANSFORM-20101");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void MapSpecToConfigurationRefusesAnUnknownDirection()
    {
        // Arrange
        var spec = new FakeTransformOperationSpec
        {
            Name = "Order",
            OperationType = "Sort",
            SortFields = [new FakeSortFieldSpec("Points", "Up")],
        };

        // Act
        var result = _sut.MapSpecToConfiguration(spec, new PipelineTransformConfiguration(), NullLogger.Instance);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }
}
