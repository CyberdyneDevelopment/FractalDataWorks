using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fdw.Services.Etl.Abstractions;
using Fdw.Services.Etl.Tests.Pipelines;
using Fdw.Services.Etl.Transforms;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Tests.Transforms;

/// <summary>
/// Tests for <see cref="UnionTransformType"/> (FDW-1120 phase 4): the rows of the union source are appended after the records
/// that reach the stage, each destination field takes its source field, and a Union that cannot say what it appends, cannot
/// read its source, or runs one record at a time fails loudly instead of appending nothing.
/// </summary>
public sealed class UnionTransformTypeTests
{
    private readonly UnionTransformType _sut = new();

    private static TransformContext Context(object? gateway) =>
        new(Guid.NewGuid(), NullLogger.Instance, new Dictionary<string, object?>(), fieldTransformChains: new Dictionary<Guid, IReadOnlyList<IFieldMappingTransformation>>(), dataGateway: gateway);

    private static object Gateway(IReadOnlyList<Dictionary<string, object?>> source, List<string> events, bool failReads = false) =>
        new PagedSourceGateway(source, events, failReads).Provider.ByName("Main");

    private static PipelineTransformFieldMappingConfiguration Mapping(string source, string destination) => new()
    {
        Name = destination,
        SourceField = source,
        DestinationField = destination
    };

    private static PipelineTransformConfiguration Union(string? dataSet, params PipelineTransformFieldMappingConfiguration[] mappings) => new()
    {
        Id = Guid.NewGuid(),
        Name = "AddArchive",
        OperationType = "Union",
        ExecutionOrder = 1,
        UnionSourceDataSet = dataSet,
        FieldMappings = [.. mappings]
    };

    private static List<IDictionary<string, object?>> Records(params (string Label, int Points)[] rows) =>
        [.. rows.Select(r => (IDictionary<string, object?>)new Dictionary<string, object?> { ["Label"] = r.Label, ["Points"] = r.Points })];

    private static List<Dictionary<string, object?>> ArchiveRows(params (string Name, int Score)[] rows) =>
        [.. rows.Select(r => new Dictionary<string, object?> { ["Name"] = r.Name, ["Score"] = r.Score })];

    private static string[] Labels(IEnumerable<IDictionary<string, object?>> rows) => rows.Select(r => (string)r["Label"]!).ToArray();

    private static FakeTransformOperationSpec Spec(string? unionSource, params PipelineTransformFieldMappingConfiguration[] mappings) => new()
    {
        Name = "AddArchive",
        OperationType = "Union",
        UnionSource = unionSource,
        FieldMappings = [.. mappings]
    };

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task TheRowsOfTheUnionSourceFollowTheRecordsAndEachDestinationFieldTakesItsSourceField()
    {
        // Arrange
        var gateway = Gateway(ArchiveRows(("old1", 1), ("old2", 2)), []);
        var stage = Union("Archive", Mapping("Name", "Label"), Mapping("Score", "Points"));

        // Act
        var result = await _sut.TransformBatch(Records(("a", 5), ("b", 6)), stage, Context(gateway), TestContext.Current.CancellationToken);

        // Assert: the main records first, then the appended rows under the destination field names
        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        Labels(result.Value!).ShouldBe(new[] { "a", "b", "old1", "old2" });
        result.Value!.Last()["Points"].ShouldBe(2);
        result.Value!.Last().ContainsKey("Name").ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ADuplicateOfAMainRecordIsKeptBecauseAUnionIsUnionAll()
    {
        // Arrange
        var gateway = Gateway(ArchiveRows(("a", 5)), []);
        var stage = Union("Archive", Mapping("Name", "Label"), Mapping("Score", "Points"));

        // Act
        var result = await _sut.TransformBatch(Records(("a", 5)), stage, Context(gateway), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        Labels(result.Value!).ShouldBe(new[] { "a", "a" });
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task TheUnionSourceIsReadPageByPageUntilAShortPage()
    {
        // Arrange: 1,500 rows is a full page of 1,000 and a short page of 500
        var source = Enumerable.Range(0, 1500)
            .Select(i => new Dictionary<string, object?> { ["Name"] = $"n{i}", ["Score"] = i })
            .ToList();
        var events = new List<string>();
        var stage = Union("Archive", Mapping("Name", "Label"), Mapping("Score", "Points"));

        // Act
        var result = await _sut.TransformBatch(Records(("a", 5)), stage, Context(Gateway(source, events)), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        result.Value!.Count().ShouldBe(1501);
        events.ShouldBe(new[] { "read@0", "read@1000" });
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AnEmptyUnionSourceAppendsNothing()
    {
        // Arrange
        var stage = Union("Archive", Mapping("Name", "Label"));

        // Act
        var result = await _sut.TransformBatch(Records(("a", 5)), stage, Context(Gateway([], [])), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        Labels(result.Value!).ShouldBe(new[] { "a" });
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AUnionSourceThatCannotBeReadFailsTheStage()
    {
        // Arrange
        var stage = Union("Archive", Mapping("Name", "Label"));

        // Act
        var result = await _sut.TransformBatch(Records(("a", 5)), stage, Context(Gateway(ArchiveRows(("x", 1)), [], failReads: true)), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ARowThatLacksAMappedSourceFieldFailsTheStageRatherThanFillingItIn()
    {
        // Arrange: the mapping reads Missing, which no row has
        var stage = Union("Archive", Mapping("Missing", "Label"));

        // Act
        var result = await _sut.TransformBatch(Records(("a", 5)), stage, Context(Gateway(ArchiveRows(("x", 1)), [])), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ARunWithNoDataGatewayFailsTheStage()
    {
        // Arrange
        var stage = Union("Archive", Mapping("Name", "Label"));

        // Act
        var result = await _sut.TransformBatch(Records(("a", 5)), stage, Context(null), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public async Task AUnionThatDoesNotStateItsSourceIsRefusedAtRunTime(string? dataSet)
    {
        // Act
        var result = await _sut.TransformBatch(
            Records(("a", 5)), Union(dataSet, Mapping("Name", "Label")), Context(Gateway(ArchiveRows(("x", 1)), [])), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ARunThatHandlesOneRecordAtATimeFailsTheUnionLoudly()
    {
        // Arrange
        var stage = Union("Archive", Mapping("Name", "Label"));
        var record = new Dictionary<string, object?> { ["Label"] = "a" };

        // Act
        var result = await _sut.Transform(record, stage, Context(Gateway(ArchiveRows(("x", 1)), [])), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ABranchPathCanHoldAUnionWhichAppendsToTheRecordsOfThatPath()
    {
        // Arrange: only Positive records take the path; the appended rows join them there and are not tested against the condition
        var path = new PipelineTransformPathConfiguration
        {
            Name = "Positive",
            Condition = "Points > 0",
            Ordinal = 1,
            Stages = [Union("Archive", Mapping("Name", "Label"), Mapping("Score", "Points"))]
        };
        var branch = new PipelineTransformConfiguration
        {
            Id = Guid.NewGuid(),
            Name = "Route",
            OperationType = "Branch",
            Unmatched = "Pass",
            Paths = [path]
        };
        var gateway = Gateway(ArchiveRows(("old", -9)), []);

        // Act
        var result = await new BranchTransformType().TransformBatch(Records(("a", 5), ("c", -1)), branch, Context(gateway), TestContext.Current.CancellationToken);

        // Assert: the path's records and the rows appended to them, then the record no path took
        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        Labels(result.Value!).ShouldBe(new[] { "a", "old", "c" });
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void TheKindOwnsTheUnionSourceAndTheFieldMappingsAndNeedsTheWholeInput()
    {
        // Assert
        _sut.Members.Select(member => member.Name).ShouldBe(new[] { "unionSource", "fieldMappings" });
        _sut.Members.ShouldAllBe(member => member.IsRequired);
        _sut.RequiresCompleteInput.ShouldBeTrue();
        _sut.CanFilterRecords.ShouldBeFalse();
        _sut.PipelineTypes.ShouldBe(new[] { "BatchCopy" });
        StageRunner.RequiresCompleteInput([Union("Archive", Mapping("Name", "Label"))]).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void AUnionStatesItsSourceAndItsMappingsOnTheConfigurationItWasMappedTo()
    {
        // Arrange
        var target = new PipelineTransformConfiguration { Name = "AddArchive", OperationType = "Union" };
        var spec = Spec("Archive", Mapping("Name", "Label"), Mapping("Score", "Points"));

        // Act
        var result = _sut.MapSpecToConfiguration(spec, target, NullLogger.Instance);

        // Assert
        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        target.UnionSourceDataSet.ShouldBe("Archive");
        target.FieldMappings.Select(m => (m.SourceField, m.DestinationField)).ShouldBe(new[] { ("Name", "Label"), ("Score", "Points") });
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void AUnionThatDoesNotStateItsSourceIsRefusedWhenMapped(string? unionSource)
    {
        // Act
        var result = _sut.MapSpecToConfiguration(Spec(unionSource, Mapping("Name", "Label")), new PipelineTransformConfiguration(), NullLogger.Instance);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void AUnionWithNoMappingIsRefused()
    {
        // Act
        var result = _sut.MapSpecToConfiguration(Spec("Archive"), new PipelineTransformConfiguration(), NullLogger.Instance);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    [InlineData("blankSource")]
    [InlineData("blankDestination")]
    [InlineData("defaultValue")]
    [InlineData("targetType")]
    [InlineData("isRequired")]
    [InlineData("isEnabled")]
    public void AMappingThatStatesMoreThanASourceAndADestinationIsRefused(string problem)
    {
        // Arrange
        var mapping = Mapping("Name", "Label");
        switch (problem)
        {
            case "blankSource": mapping.SourceField = " "; break;
            case "blankDestination": mapping.DestinationField = string.Empty; break;
            case "defaultValue": mapping.DefaultValue = "none"; break;
            case "targetType": mapping.TargetType = "string"; break;
            case "isRequired": mapping.IsRequired = true; break;
            case "isEnabled": mapping.IsEnabled = false; break;
        }

        // Act
        var result = _sut.MapSpecToConfiguration(Spec("Archive", mapping), new PipelineTransformConfiguration(), NullLogger.Instance);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void TwoMappingsToTheSameDestinationAreRefused()
    {
        // Act
        var result = _sut.MapSpecToConfiguration(
            Spec("Archive", Mapping("Name", "Label"), Mapping("Score", "label")), new PipelineTransformConfiguration(), NullLogger.Instance);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void ARequestThatIsNotAUnionSpecIsRefused()
    {
        // Arrange: a plain spec, which cannot state a union source
        var plain = new PlainUnionlessSpec();

        // Act
        var result = _sut.MapSpecToConfiguration(plain, new PipelineTransformConfiguration(), NullLogger.Instance);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    private sealed class PlainUnionlessSpec : ITransformOperationSpec
    {
        public string Name { get; } = "AddArchive";

        public string OperationType { get; } = "Union";

        public int ExecutionOrder { get; }

        public bool IsEnabled { get; } = true;

        public IReadOnlyList<IFieldMapping> FieldMappings { get; } = [];

        public IReadOnlyList<string> GroupByFields { get; } = [];

        public IReadOnlyList<IAggregationSpec> Aggregations { get; } = [];

        public ILookupSpec? Lookup { get; }

        public IReadOnlyList<ICalculationSpec> ComputedColumns { get; } = [];

        public string? FilterExpression { get; }
    }
}
