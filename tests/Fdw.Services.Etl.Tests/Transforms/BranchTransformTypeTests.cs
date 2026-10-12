using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fdw.Services.Etl.Transforms;
using Microsoft.Extensions.Logging.Abstractions;
using Fdw.Services.Etl.Abstractions;

namespace Fdw.Services.Etl.Tests.Transforms;

/// <summary>
/// Tests for <see cref="BranchTransformType"/> (FDW-1120): a record takes the first path whose condition it meets, the stages
/// of that path run on the records it took, the outputs follow in path order, and a Branch that cannot say what an unmatched
/// record causes, or has no path, or has a condition it cannot read, is refused rather than guessed.
/// </summary>
public sealed class BranchTransformTypeTests
{
    private readonly BranchTransformType _sut = new();

    private static TransformContext Context(params (string Name, object? Value)[] parameters)
    {
        var values = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var (name, value) in parameters)
            values[name] = value;
        return new TransformContext(Guid.NewGuid(), NullLogger.Instance, new Dictionary<string, object?>(), fieldTransformChains: new Dictionary<Guid, IReadOnlyList<IFieldMappingTransform>>(), parameters: values);
    }

    private static PipelineTransformPathConfiguration Path(string name, string condition, int ordinal, params PipelineTransformConfiguration[] stages) => new()
    {
        Name = name,
        Condition = condition,
        Ordinal = ordinal,
        Stages = [.. stages]
    };

    private static PipelineTransformConfiguration Branch(string? unmatched, params PipelineTransformPathConfiguration[] paths) => new()
    {
        Id = Guid.NewGuid(),
        Name = "Route",
        OperationType = "Branch",
        Unmatched = unmatched,
        Paths = [.. paths]
    };

    private static PipelineTransformConfiguration Rejecting(string name, string expression, int order = 1) => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        OperationType = "Validate",
        ExecutionOrder = order,
        OnFail = "Reject",
        Checks = [new PipelineTransformCheckConfiguration { Name = "Check", Expression = expression, Ordinal = 1 }]
    };

    private static Dictionary<string, object?> Row(string label, int points) => new() { ["Label"] = label, ["Points"] = points };

    private static string[] Labels(IEnumerable<IDictionary<string, object?>> rows) => rows.Select(r => (string)r["Label"]!).ToArray();

    private static List<IDictionary<string, object?>> Rows(params (string Label, int Points)[] rows) =>
        [.. rows.Select(r => (IDictionary<string, object?>)Row(r.Label, r.Points))];

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ARecordTakesTheFirstPathItMeetsAndTheOutputsFollowInPathOrder()
    {
        // Arrange: b meets both conditions and must take High only; c meets neither
        var stage = Branch("Pass", Path("High", "Points >= 10", 1), Path("Positive", "Points > 0", 2));

        // Act
        var result = await _sut.TransformBatch(Rows(("a", 5), ("b", 20), ("c", -1)), stage, Context(), TestContext.Current.CancellationToken);

        // Assert: High's records, then Positive's, then the unmatched that Pass lets on
        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        Labels(result.Value!).ShouldBe(new[] { "b", "a", "c" });
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task PathsAreTriedInOrdinalOrderNotInTheOrderTheyAreStored()
    {
        // Arrange: stored Positive first, but its ordinal makes it second
        var stage = Branch("Pass", Path("Positive", "Points > 0", 2), Path("High", "Points >= 10", 1));

        // Act
        var result = await _sut.TransformBatch(Rows(("b", 20)), stage, Context(), TestContext.Current.CancellationToken);

        // Assert: b took High (ordinal 1); nothing is left for Positive
        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        Labels(result.Value!).ShouldBe(new[] { "b" });
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task RejectDropsAnUnmatchedRecordAndReportsTheBranch()
    {
        // Arrange
        var stage = Branch("Reject", Path("Positive", "Points > 0", 1));
        var context = Context();

        // Act
        var result = await _sut.TransformBatch(Rows(("a", 5), ("c", -1)), stage, context, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        Labels(result.Value!).ShouldBe(new[] { "a" });
        context.Errors.Count.ShouldBe(1);
        context.Errors[0].Message.ShouldContain("Route");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task APathsStagesRunOnlyOnTheRecordsThePathTook()
    {
        // Arrange: the Small check would reject b (20) if it ran on every record; it sits on the Positive path only
        var stage = Branch("Pass",
            Path("High", "Points >= 10", 1),
            Path("Positive", "Points > 0", 2, Rejecting("Small", "Points < 5")));
        var context = Context();

        // Act
        var result = await _sut.TransformBatch(Rows(("a", 5), ("b", 20), ("d", 3)), stage, context, TestContext.Current.CancellationToken);

        // Assert: a (5) fails Small and is rejected, d (3) passes, b took the other path untouched
        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        Labels(result.Value!).ShouldBe(new[] { "b", "d" });
        context.Errors.Count.ShouldBe(1);
        context.Errors[0].Message.ShouldContain("Small");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ABranchInsideAPathRoutesTheRecordsThePathTook()
    {
        // Arrange: the inner branch splits the Positive records into Big and the unmatched rest
        var inner = Branch("Reject", Path("Big", "Points >= 10", 1));
        inner.Name = "Inner";
        inner.ExecutionOrder = 1;
        var stage = Branch("Pass", Path("Positive", "Points > 0", 1, inner));
        var context = Context();

        // Act
        var result = await _sut.TransformBatch(Rows(("a", 5), ("b", 20), ("c", -1)), stage, context, TestContext.Current.CancellationToken);

        // Assert: a is rejected by the inner branch, b passes it, c matched no outer path and passes
        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        Labels(result.Value!).ShouldBe(new[] { "b", "c" });
        context.Errors.Single().Message.ShouldContain("Inner");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task APathConditionMayNameARunParameter()
    {
        // Arrange
        var stage = Branch("Reject", Path("Over", "Points > {{limit}}", 1));

        // Act
        var result = await _sut.TransformBatch(Rows(("a", 5), ("b", 20)), stage, Context(("limit", 10L)), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        Labels(result.Value!).ShouldBe(new[] { "b" });
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task APerRecordRunReturnsTheRecordThePathKeptOrNothingWhenItIsRejected()
    {
        // Arrange
        var stage = Branch("Reject", Path("Positive", "Points > 0", 1));
        var context = Context();

        // Act
        var kept = await _sut.Transform(Row("a", 5), stage, context, TestContext.Current.CancellationToken);
        var dropped = await _sut.Transform(Row("c", -1), stage, context, TestContext.Current.CancellationToken);

        // Assert
        kept.IsSuccess.ShouldBeTrue(kept.CurrentMessage);
        kept.Value!["Label"].ShouldBe("a");
        dropped.IsSuccess.ShouldBeTrue(dropped.CurrentMessage);
        dropped.Value.ShouldBeNull();
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Ignore")]
    public async Task ABranchThatDoesNotSayWhatAnUnmatchedRecordCausesIsRefused(string? unmatched)
    {
        // Arrange
        var stage = Branch(unmatched, Path("Positive", "Points > 0", 1));

        // Act
        var result = await _sut.TransformBatch(Rows(("a", 5)), stage, Context(), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ABranchWithNoPathIsRefused()
    {
        // Act
        var result = await _sut.TransformBatch(Rows(("a", 5)), Branch("Pass"), Context(), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task APathConditionThatCannotBeReadIsRefusedNotPassed()
    {
        // Act
        var result = await _sut.TransformBatch(
            Rows(("a", 5)), Branch("Pass", Path("Broken", "garbage expression", 1)), Context(), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void ABranchListsItsMembersAndHowDeepItMayNest()
    {
        // Assert
        _sut.Members.Select(member => member.Name).ShouldBe(new[] { "paths", "unmatched" });
        _sut.MaxDepth.ShouldBe(3);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void AStageBelowABranchThatNeedsTheWholeInputStopsTheRunStreaming()
    {
        // Arrange: an Aggregate inside a path
        var aggregate = new PipelineTransformConfiguration { Id = Guid.NewGuid(), Name = "Total", OperationType = "Aggregate", ExecutionOrder = 1 };
        var stage = Branch("Pass", Path("Positive", "Points > 0", 1, aggregate));
        var plain = Branch("Pass", Path("Positive", "Points > 0", 1));

        // Assert
        StageRunner.RequiresCompleteInput([stage]).ShouldBeTrue();
        StageRunner.RequiresCompleteInput([plain]).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void StagesAreOrderedByExecutionOrderAndADisabledStageIsLeftOut()
    {
        // Arrange
        var first = new PipelineTransformConfiguration { Id = Guid.NewGuid(), Name = "First", OperationType = "Filter", ExecutionOrder = 1 };
        var second = new PipelineTransformConfiguration { Id = Guid.NewGuid(), Name = "Second", OperationType = "Filter", ExecutionOrder = 2 };
        var off = new PipelineTransformConfiguration { Id = Guid.NewGuid(), Name = "Off", OperationType = "Filter", ExecutionOrder = 0, IsEnabled = false };

        // Act
        var ordered = StageRunner.Ordered([second, off, first]);

        // Assert
        ordered.Select(stage => stage.Name).ShouldBe(new[] { "First", "Second" });
        StageRunner.Ordered(null).ShouldBeEmpty();
    }
}
