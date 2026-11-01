using System.Collections.Generic;
using System.Linq;
using Fdw.Commands.Data.Abstractions;
using Fdw.Commands.Data.Updates;
using Fdw.Commands.Data.Upserts;
using Fdw.Commands.Data.ValueSources;

namespace Fdw.Commands.Data.Tests.Expansions;

/// <summary>
/// A bulk command expands into the single command for each of its rows, which is what a connection with no bulk
/// mechanism for it runs.
/// </summary>
public sealed class CommandExpansionTests
{
    private static readonly IStorageContainer Container = new Mock<IStorageContainer>().Object;

    private static Dictionary<string, object?> Row(int id, string name) => new() { ["Id"] = id, ["Name"] = name };

    [Fact]
    public void ABulkUpsertExpandsToOneUpsertForEachRowWithTheSameOptions()
    {
        var options = new UpsertOptions(["Id"], ["Name"], ["Id", "Name"]);
        var command = new BulkUpsertCommand<Dictionary<string, object?>>([Row(1, "a"), Row(2, "b"), Row(3, "c")], options);

        var expansion = command.Expand(Container);

        expansion.IsSuccess.ShouldBeTrue();
        var upserts = expansion.Value!.Select(step => step.Command.ShouldBeOfType<UpsertCommand<Dictionary<string, object?>>>()).ToList();
        upserts.Count.ShouldBe(3);
        upserts.ShouldAllBe(upsert => ReferenceEquals(upsert.Options, options));
        upserts.Select(upsert => upsert.Data.Single()["Name"]).ShouldBe(["a", "b", "c"]);
    }

    [Fact]
    public void ABulkUpsertOfNoRowsExpandsToNothing()
    {
        var command = new BulkUpsertCommand<Dictionary<string, object?>>([], new UpsertOptions(["Id"], [], ["Id"]));

        command.Expand(Container).Value!.ShouldBeEmpty();
    }

    [Fact]
    public void ABulkUpdateExpandsToOneUpdateForEachRowFilteredOnItsKey()
    {
        var command = new BulkUpdateCommand<Dictionary<string, object?>>([Row(1, "a"), Row(2, "b")], new BulkUpdateOptions(["Id"]));

        var expansion = command.Expand(Container);

        expansion.IsSuccess.ShouldBeTrue();
        var updates = expansion.Value!.Select(step => step.Command.ShouldBeOfType<UpdateCommand<Dictionary<string, object?>>>()).ToList();
        updates.Count.ShouldBe(2);
        var condition = updates[1].Filter!.Root.ShouldBeOfType<FilterCondition>();
        condition.PropertyName.ShouldBe("Id");
        condition.Value.ShouldBe(2);
        updates[1].Data["Name"].ShouldBe("b");
    }

    [Fact]
    public void ABulkUpdateWithTwoKeyColumnsFiltersOnBothTogether()
    {
        var command = new BulkUpdateCommand<Dictionary<string, object?>>([Row(1, "a")], new BulkUpdateOptions(["Id", "Name"]));

        var group = command.Expand(Container).Value!.Single().Command.ShouldBeOfType<UpdateCommand<Dictionary<string, object?>>>()
            .Filter!.Root.ShouldBeOfType<FilterGroup>();

        group.Operator.ShouldBe(LogicalOperator.And);
        group.Nodes.Select(node => ((FilterCondition)node).PropertyName).ShouldBe(["Id", "Name"]);
    }

    [Fact]
    public void ABulkUpdateWhoseRowLacksAKeyValueFailsNamingTheKey()
    {
        var command = new BulkUpdateCommand<Dictionary<string, object?>>([new Dictionary<string, object?> { ["Name"] = "a" }], new BulkUpdateOptions(["Id"]));

        var expansion = command.Expand(Container);

        expansion.IsFailure.ShouldBeTrue();
        expansion.CurrentMessage.ShouldNotBeNull().ShouldContain("Id");
    }

    [Fact]
    public void ABulkUpdateOfARowWithAnEntityReadsTheKeyFromItsProperty()
    {
        var command = new BulkUpdateCommand<Entity>([new Entity { Id = 7 }], new BulkUpdateOptions(["Id"]));

        command.Expand(Container).Value!.Single().Command.ShouldBeOfType<UpdateCommand<Entity>>()
            .Filter!.Root.ShouldBeOfType<FilterCondition>().Value.ShouldBe(7);
    }

    [Fact]
    public void AnUpsertExpandsToADeleteByKeyAndAnInsertForEachRowAndOnlyTheInsertsCount()
    {
        var command = new UpsertCommand<Dictionary<string, object?>>([Row(1, "a"), Row(2, "b")], new UpsertOptions(["Id"], [], ["Id", "Name"]));

        var expansion = command.Expand(Container);

        expansion.IsSuccess.ShouldBeTrue();
        var steps = expansion.Value!;
        steps.Count.ShouldBe(4);
        steps[0].Command.ShouldBeOfType<DeleteCommand>().Filter!.Root.ShouldBeOfType<FilterCondition>().Value.ShouldBe(1);
        steps[1].Command.ShouldBeOfType<InsertCommand<Dictionary<string, object?>>>().Data.Single()["Name"].ShouldBe("a");
        steps[2].Command.ShouldBeOfType<DeleteCommand>().Filter!.Root.ShouldBeOfType<FilterCondition>().Value.ShouldBe(2);
        steps.Select(step => step.CountsAsAffected).ShouldBe([false, true, false, true]);
    }

    [Fact]
    public void AnUpsertWithVersioningIsRefusedBecauseDeletingWouldEraseHistory()
    {
        var options = new UpsertOptions(["Id"], [], ["Id", "Name"]) { Versioning = new VersioningOptions("From", "To") };

        var expansion = new UpsertCommand<Dictionary<string, object?>>([Row(1, "a")], options).Expand(Container);

        expansion.IsFailure.ShouldBeTrue();
        expansion.CurrentMessage.ShouldNotBeNull().ShouldContain("versioning");
    }

    [Fact]
    public void AnUpsertWithValueSourcesIsRefusedBecauseTheDatabaseSuppliesThem()
    {
        var options = new UpsertOptions(["Id"], [], ["Id", "Name"])
        {
            ValueSources = [new ColumnValueBinding("LoadedAt", new CurrentTimestampValueSource())],
        };

        var expansion = new UpsertCommand<Dictionary<string, object?>>([Row(1, "a")], options).Expand(Container);

        expansion.IsFailure.ShouldBeTrue();
        expansion.CurrentMessage.ShouldNotBeNull().ShouldContain("value sources");
    }

    [Fact]
    public void AnUpsertWhoseRowLacksAKeyValueFailsNamingTheKey()
    {
        var options = new UpsertOptions(["Id"], [], ["Id", "Name"]);

        var expansion = new UpsertCommand<Dictionary<string, object?>>([new Dictionary<string, object?> { ["Name"] = "a" }], options).Expand(Container);

        expansion.IsFailure.ShouldBeTrue();
        expansion.CurrentMessage.ShouldNotBeNull().ShouldContain("Id");
    }

    [Fact]
    public void AnUpsertIsRefusedWhenTheContainerHasAColumnTheRowsDoNotCarry()
    {
        var container = ContainerOf(("Id", false), ("Name", false), ("Notes", false));
        var options = new UpsertOptions(["Id"], [], ["Id", "Name"]);

        var expansion = new UpsertCommand<Dictionary<string, object?>>([Row(1, "a")], options).Expand(container);

        expansion.IsFailure.ShouldBeTrue();
        expansion.CurrentMessage.ShouldNotBeNull().ShouldContain("Notes");
    }

    [Fact]
    public void AColumnTheDatabaseProvidesDoesNotStopAnUpsertBeingComposed()
    {
        var container = ContainerOf(("Id", false), ("Name", false), ("CreatedAt", true));
        var options = new UpsertOptions(["Id"], [], ["Id", "Name"]);

        new UpsertCommand<Dictionary<string, object?>>([Row(1, "a")], options).Expand(container).IsSuccess.ShouldBeTrue();
    }

    private static IDataContainer ContainerOf(params (string Name, bool SystemProvided)[] columns)
    {
        var fields = columns.Select(column =>
        {
            var field = new Mock<IField>();
            field.Setup(f => f.Name).Returns(column.Name);
            field.Setup(f => f.IsSystemProvided).Returns(column.SystemProvided);
            return field.Object;
        }).ToList();
        var schema = new Mock<IContainerSchema>();
        schema.Setup(s => s.Fields).Returns(fields);
        var container = new Mock<IDataContainer>();
        container.Setup(c => c.Schema).Returns(schema.Object);
        return container.Object;
    }

    [Fact]
    public void BulkUpdateOptionsNeedAKeyColumnNamedOnce()
    {
        new BulkUpdateOptions(["Id"]).FirstProblem().ShouldBeNull();
        new BulkUpdateOptions([]).FirstProblem().ShouldNotBeNull();
        new BulkUpdateOptions(["Id", "id"]).FirstProblem().ShouldNotBeNull();
        new BulkUpdateOptions([" "]).FirstProblem().ShouldNotBeNull();
    }

    [Fact]
    public void TheBulkCommandsHaveTheirOwnCommandTypes()
    {
        new BulkUpsertCommand<Dictionary<string, object?>>([], new UpsertOptions(["Id"], [], ["Id"])).CommandType.ShouldBe("BulkUpsert");
        new BulkUpdateCommand<Dictionary<string, object?>>([], new BulkUpdateOptions(["Id"])).CommandType.ShouldBe("BulkUpdate");
    }

    private sealed class Entity
    {
        public int Id { get; set; }
    }
}
