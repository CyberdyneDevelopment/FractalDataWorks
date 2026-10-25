using System.Collections.Generic;
using System.Linq;
using Fdw.Commands.Data.Abstractions;
using Fdw.Commands.Data.Upserts;

namespace Fdw.Commands.Data.Tests.Expansions;

/// <summary>
/// An upsert of more rows than a connection runs serially replaces them together: the matching rows are deleted by key in
/// groups, then every row goes in by one insert that the connection takes the bulk way. Up to that many rows, or on a
/// connection that states none, each row is replaced on its own.
/// </summary>
public sealed class BulkReplaceExpansionTests
{
    private static readonly IStorageContainer Container = new Mock<IStorageContainer>().Object;

    private static Dictionary<string, object?> Row(int id, string name = "n") => new() { ["Id"] = id, ["Name"] = name };

    private static UpsertCommand<Dictionary<string, object?>> UpsertOf(int rows, params string[] keys)
        => new(Enumerable.Range(1, rows).Select(i => Row(i)).ToList(), new UpsertOptions(keys.Length == 0 ? ["Id"] : keys, ["Name"], ["Id", "Name"]));

    private static IEnumerable<int> KeysIn(ExpandedCommand step)
        => Conditions(step).Select(condition => (int)condition.Value!);

    private static IEnumerable<FilterCondition> Conditions(ExpandedCommand step)
    {
        var root = step.Command.ShouldBeOfType<DeleteCommand>().Filter!.Root;
        return root is FilterGroup group ? group.Nodes.OfType<FilterCondition>() : [root.ShouldBeOfType<FilterCondition>()];
    }

    [Fact]
    public void AnUpsertOfMoreRowsThanTheNumberDeletesTheirKeysInGroupsAndInsertsEveryRowOnce()
    {
        var expansion = UpsertOf(5).Expand(Container, maxSerialInsert: 3);

        expansion.IsSuccess.ShouldBeTrue(expansion.CurrentMessage);
        var steps = expansion.Value!;
        steps.Count.ShouldBe(3);
        KeysIn(steps[0]).ShouldBe([1, 2, 3]);
        KeysIn(steps[1]).ShouldBe([4, 5]);
        steps[0].CountsAsAffected.ShouldBeFalse();
        steps[1].CountsAsAffected.ShouldBeFalse();
        var insert = steps[2].Command.ShouldBeOfType<InsertCommand<Dictionary<string, object?>>>();
        insert.Data.Count().ShouldBe(5);
        insert.ReroutesToBulk(3).ShouldBeTrue();
        steps[2].CountsAsAffected.ShouldBeTrue();
    }

    [Fact]
    public void AGroupOfOneKeyIsTheConditionItself()
    {
        var steps = UpsertOf(4).Expand(Container, maxSerialInsert: 3).Value!;

        steps[1].Command.ShouldBeOfType<DeleteCommand>().Filter!.Root.ShouldBeOfType<FilterCondition>().Value.ShouldBe(4);
    }

    [Fact]
    public void TheGroupsHoldNoMoreParametersThanTheNumberWhenTheKeyHasTwoColumns()
    {
        var steps = UpsertOf(5, "Id", "Name").Expand(Container, maxSerialInsert: 4).Value!;

        steps.Count.ShouldBe(4);
        var first = steps[0].Command.ShouldBeOfType<DeleteCommand>().Filter!.Root.ShouldBeOfType<FilterGroup>();
        first.Operator.ShouldBe(LogicalOperator.Or);
        first.Nodes.ShouldAllBe(node => node is FilterGroup);
        first.Nodes.Count.ShouldBe(2);
    }

    [Fact]
    public void AnUpsertOfTheNumberOfRowsOrFewerReplacesEachRowOnItsOwn()
    {
        var steps = UpsertOf(3).Expand(Container, maxSerialInsert: 3).Value!;

        steps.Count.ShouldBe(6);
        steps.Where((_, index) => index % 2 == 0).ShouldAllBe(step => step.Command is DeleteCommand);
        steps.Where((_, index) => index % 2 == 1).ShouldAllBe(step => step.Command is InsertCommand<Dictionary<string, object?>>);
    }

    [Fact]
    public void AnUpsertForAConnectionThatStatesNoNumberReplacesEachRowOnItsOwn()
    {
        UpsertOf(50).Expand(Container).Value!.Count.ShouldBe(100);
    }

    [Fact]
    public void TwoRowsWithTheSameKeyAreRefusedInTheBulkFormNamingTheKey()
    {
        var command = new UpsertCommand<Dictionary<string, object?>>(
            [Row(1), Row(2), Row(2, "again"), Row(3)], new UpsertOptions(["Id"], ["Name"], ["Id", "Name"]));

        var expansion = command.Expand(Container, maxSerialInsert: 3);

        expansion.IsFailure.ShouldBeTrue();
        expansion.CurrentMessage.ShouldNotBeNull().ShouldContain("(2)");
    }

    [Fact]
    public void ARowWithNoKeyValueIsRefusedInTheBulkFormNamingTheKey()
    {
        var command = new UpsertCommand<Dictionary<string, object?>>(
            [Row(1), new Dictionary<string, object?> { ["Name"] = "x" }, Row(3), Row(4)], new UpsertOptions(["Id"], ["Name"], ["Id", "Name"]));

        var expansion = command.Expand(Container, maxSerialInsert: 3);

        expansion.IsFailure.ShouldBeTrue();
        expansion.CurrentMessage.ShouldNotBeNull().ShouldContain("Id");
    }

    [Fact]
    public void ABulkUpsertOfMoreRowsThanTheNumberIsOneUpsertHoldingEveryRow()
    {
        var command = new BulkUpsertCommand<Dictionary<string, object?>>(
            Enumerable.Range(1, 5).Select(i => Row(i)).ToList(), new UpsertOptions(["Id"], ["Name"], ["Id", "Name"]));

        var steps = command.Expand(Container, maxSerialInsert: 3).Value!;

        steps.Count.ShouldBe(1);
        steps[0].Command.ShouldBeOfType<UpsertCommand<Dictionary<string, object?>>>().Data.Count().ShouldBe(5);
    }

    [Fact]
    public void ABulkUpsertOfTheNumberOfRowsOrFewerIsOneUpsertForEachRow()
    {
        var command = new BulkUpsertCommand<Dictionary<string, object?>>(
            Enumerable.Range(1, 3).Select(i => Row(i)).ToList(), new UpsertOptions(["Id"], ["Name"], ["Id", "Name"]));

        command.Expand(Container, maxSerialInsert: 3).Value!.Count.ShouldBe(3);
    }
}
