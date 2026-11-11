using System.Collections.Generic;

namespace Fdw.Commands.Data.Abstractions.Tests.Commands;

/// <summary>
/// The answer of a write that reports its unmatched keys (FDW-4988) is read from a summary set and a key set, and every other shape is refused by its own
/// code, never read as zero unmatched.
/// </summary>
public sealed class BulkWriteAnswerTests
{
    private static IReadOnlyList<IReadOnlyDictionary<string, object?>> Summary(object? affected, object? unmatched) =>
        [new Dictionary<string, object?> { [BulkWriteAnswer.AffectedColumn] = affected, [BulkWriteAnswer.UnmatchedColumn] = unmatched }];

    private static IReadOnlyList<IReadOnlyDictionary<string, object?>> Keys(params string[] codes) =>
        [.. System.Linq.Enumerable.Select(codes, code => (IReadOnlyDictionary<string, object?>)new Dictionary<string, object?> { ["Code"] = code })];

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void TheAnswerIsTheSummaryAndTheKeys()
    {
        var answer = BulkWriteAnswer.From("BulkUpdate", [Summary(7L, 3L), Keys("A", "B")], 2);

        answer.IsSuccess.ShouldBeTrue(answer.CurrentMessage);
        answer.Value!.RowsAffected.ShouldBe(7L);
        answer.Value.UnmatchedKeys.ShouldBe(3L);
        answer.Value.UnmatchedSample.Count.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AnIntegerOrAWholeDecimalCountIsRead()
    {
        BulkWriteAnswer.From("BulkDelete", [Summary(4, 0m), Keys()], 0).Value!.RowsAffected.ShouldBe(4L);
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    [InlineData(1)]
    [InlineData(3)]
    public void AnAnswerThatIsNotTwoSetsIsRefused(int sets)
    {
        var all = new List<IReadOnlyList<IReadOnlyDictionary<string, object?>>> { Summary(1L, 0L), Keys(), Keys() };

        BulkWriteAnswer.From("BulkUpdate", all.GetRange(0, sets), 5).CurrentMessage.ShouldNotBeNull().ShouldContain("result set");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ASummaryThatIsNotOneRowIsRefused()
    {
        BulkWriteAnswer.From("BulkUpdate", [Keys(), Keys()], 5).IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AnUnreadableCountIsRefusedNotReadAsZero()
    {
        BulkWriteAnswer.From("BulkUpdate", [Summary(1L, null), Keys()], 5).CurrentMessage.ShouldNotBeNull().ShouldContain(BulkWriteAnswer.UnmatchedColumn);
        BulkWriteAnswer.From("BulkUpdate", [Summary("many", 0L), Keys()], 5).CurrentMessage.ShouldNotBeNull().ShouldContain(BulkWriteAnswer.AffectedColumn);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ANegativeCountIsRefused()
    {
        BulkWriteAnswer.From("BulkUpdate", [Summary(1L, -1L), Keys()], 5).IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void MoreKeysThanAskedForOrThanCountedAreRefused()
    {
        BulkWriteAnswer.From("BulkUpdate", [Summary(1L, 5L), Keys("A", "B")], 1).CurrentMessage.ShouldNotBeNull().ShouldContain("more than the 1");
        BulkWriteAnswer.From("BulkUpdate", [Summary(1L, 1L), Keys("A", "B")], 5).CurrentMessage.ShouldNotBeNull().ShouldContain("counted only 1");
    }
}
