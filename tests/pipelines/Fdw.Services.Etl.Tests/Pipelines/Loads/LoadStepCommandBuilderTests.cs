using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Commands.Data.Loads;
using Fdw.Commands.Data.ValueSources;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Services.Etl.Abstractions.OptionTypes;
using Fdw.Services.Etl.Pipelines.Loads;
using Fdw.Services.Etl.Pipelines.Loads.Builders;
using Microsoft.Extensions.Logging.Abstractions;
using CommandSoftDeleteModes = Fdw.Commands.Data.SoftDeletes.SoftDeleteModes;

namespace Fdw.Services.Etl.Tests.Pipelines.Loads;

/// <summary>
/// Tests for the load planner's command builders (FDW-1109): from a load row and the output rows, each kind builds exactly
/// the commands it should and refuses what it cannot, with nothing named that the row did not name.
/// </summary>
public sealed class LoadStepCommandBuilderTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 8, 9, 10, 11, TimeSpan.Zero);

    private static PipelineLoadColumnConfiguration Column(string role, string column, int ordinal = 0) =>
        new() { Name = column, Role = role, ColumnName = column, Ordinal = ordinal };

    private static PipelineLoadStepConfiguration Row(params PipelineLoadColumnConfiguration[] columns) =>
        new() { Name = "step", IsEnabled = true, Columns = columns };

    private static PipelineLoadValueSourceConfiguration Constant(string column, string value, string type) =>
        new() { Name = column, ColumnName = column, SourceKind = "Constant", ConstantValue = value, ConstantDataType = type };

    private static PipelineLoadValueSourceConfiguration Stamp(string column) =>
        new() { Name = column, ColumnName = column, SourceKind = "CurrentTimestamp" };

    private static PipelineLoadValueSourceConfiguration Sequence(string column) =>
        new()
        {
            Name = column,
            ColumnName = column,
            SourceKind = "NextSequenceValue",
            SequenceName = "Numbers",
            SequenceOrderColumns = [new PipelineLoadColumnConfiguration { Name = "o", Role = "SequenceOrder", ColumnName = "Id", Ordinal = 0 }],
        };

    private static List<IDictionary<string, object?>> Rows(int count = 2) =>
        Enumerable.Range(1, count)
            .Select(i => (IDictionary<string, object?>)new Dictionary<string, object?> { ["Id"] = i, ["Name"] = $"n{i}", ["Status"] = "open", ["Extra"] = "x" })
            .ToList();

    private static LoadCommandInput Input(PipelineLoadStepConfiguration row, List<IDictionary<string, object?>>? rows = null, QuerySource? source = null) =>
        new("load step 'step'", row, rows ?? Rows(), source, Now, NullLogger.Instance);

    private static IReadOnlyList<IDataCommand> Built(string kind, LoadCommandInput input)
    {
        var result = LoadStepCommandBuilders.ByName(kind).Build(input);
        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        return result.Value!;
    }

    private static void Refused(string kind, LoadCommandInput input, string code, string? contains = null)
    {
        var result = LoadStepCommandBuilders.ByName(kind).Build(input);
        result.IsSuccess.ShouldBeFalse();
        result.Messages[^1].Code.ShouldBe(code);
        if (contains is not null)
            result.CurrentMessage.ShouldNotBeNull().ShouldContain(contains);
    }

    private static IEnumerable<IDictionary<string, object?>> Data(IDataCommand command) => command switch
    {
        InsertCommand<IDictionary<string, object?>> insert => insert.Data,
        BulkUpsertCommand<IDictionary<string, object?>> upsert => upsert.Data,
        _ => throw new InvalidOperationException($"{command.GetType().Name} carries no rows"),
    };

    // ---- the collection

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void EveryLoadStepKindHasABuilderOfTheSameName()
    {
        foreach (var kind in LoadStepKinds.All())
        {
            ReferenceEquals(LoadStepCommandBuilders.ByName(kind.Name), LoadStepCommandBuilders.NotFound).ShouldBeFalse($"no builder for load step kind {kind.Name}");
        }
    }

    [Theory]
    [InlineData("Insert", new string[0])]
    [InlineData("Upsert", new string[0])]
    [InlineData("Update", new string[0])]
    [InlineData("Delete", new string[0])]
    [InlineData("Truncate", new string[0])]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void EachKindNamesTheCapabilitiesAConnectionTypeMustList(string kind, string[] expected)
    {
        LoadStepCommandBuilders.ByName(kind).RequiredCapabilities(Row()).ShouldBe(expected);
    }

    [Theory]
    [InlineData("Upsert")]
    [InlineData("Update")]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void ASoftDeleteAddsTheSoftDeleteCapability(string kind)
    {
        // Arrange
        var row = Row();
        row.SoftDeleteMode = "AbsentFromSource";

        // Act
        var names = LoadStepCommandBuilders.ByName(kind).RequiredCapabilities(row);

        // Assert
        names.ShouldBe(new[] { "SoftDelete" });
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void ADeleteNeedsTheSoftDeleteCapabilityOnlyWhenItFlagsInsteadOfRemoving()
    {
        var hard = Row();
        var soft = Row();
        soft.SoftDeleteMode = "MarkerMatches";

        LoadStepCommandBuilders.ByName("Delete").RequiredCapabilities(hard).ShouldBeEmpty();
        LoadStepCommandBuilders.ByName("Delete").RequiredCapabilities(soft).ShouldBe(new[] { "SoftDelete" });
    }

    // ---- Insert

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void InsertOfAnOwnLoadWithNoColumnsInsertsTheRowsAsTheyAre()
    {
        // Arrange
        var rows = Rows(3);

        // Act
        var commands = Built("Insert", Input(Row(), rows));

        // Assert
        commands.Count.ShouldBe(1);
        commands[0].ShouldBeOfType<InsertCommand<IDictionary<string, object?>>>();
        Data(commands[0]).Count().ShouldBe(3);
        Data(commands[0]).First().Keys.OrderBy(k => k, StringComparer.Ordinal).ShouldBe(new[] { "Extra", "Id", "Name", "Status" });
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void InsertRestrictsTheRowsToTheWriteColumnsAndAddsValueSourceColumns()
    {
        // Arrange
        var row = Row(Column("Write", "Id"), Column("Write", "Name"));
        row.ValueSources = [Constant("Batch", "7", "Int64"), Stamp("LoadedAt")];

        // Act
        var commands = Built("Insert", Input(row));

        // Assert
        var first = Data(commands[0]).First();
        first.Keys.OrderBy(k => k, StringComparer.Ordinal).ShouldBe(new[] { "Batch", "Id", "LoadedAt", "Name" });
        first["Batch"].ShouldBe(7L);
        first["LoadedAt"].ShouldBe(Now);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void InsertRefusesASequenceValueSourceBecauseTheDatabaseMustNumberTheBatch()
    {
        // Arrange
        var row = Row(Column("Write", "Id"));
        row.ValueSources = [Sequence("Number")];

        // Act / Assert
        Refused("Insert", Input(row), "PIPELINERUN-20213", "Insert");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void InsertRefusesAColumnTheRowsAndAValueSourceBothSupply()
    {
        // Arrange
        var row = Row(Column("Write", "Id"), Column("Write", "Name"));
        row.ValueSources = [Constant("Name", "z", "String")];

        // Act / Assert
        Refused("Insert", Input(row), "PIPELINERUN-20215");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void InsertRefusesAWriteColumnTheRowsLack()
    {
        Refused("Insert", Input(Row(Column("Write", "Missing"))), "PIPELINERUN-20214", "Missing");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void InsertOfNoRowsBuildsNothing()
    {
        Built("Insert", Input(Row(), [])).ShouldBeEmpty();
    }

    // ---- Upsert

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void UpsertCarriesKeyCompareAndWriteColumnsAndOnlyTheWriteColumnsOfTheRows()
    {
        // Arrange
        var row = Row(Column("Key", "Id"), Column("Compare", "Name"), Column("Write", "Id"), Column("Write", "Name", 1));

        // Act
        var commands = Built("Upsert", Input(row));

        // Assert
        commands.Count.ShouldBe(1);
        var command = commands[0].ShouldBeOfType<BulkUpsertCommand<IDictionary<string, object?>>>();
        command.Options.KeyColumns.ShouldBe(new[] { "Id" });
        command.Options.CompareColumns.ShouldBe(new[] { "Name" });
        command.Options.WriteColumns.ShouldBe(new[] { "Id", "Name" });
        command.Options.Versioning.ShouldBeNull();
        command.Options.ValueSources.ShouldBeNull();
        Data(command).First().Keys.OrderBy(k => k, StringComparer.Ordinal).ShouldBe(new[] { "Id", "Name" });
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void UpsertCarriesVersioningWithItsInsertOnlyRule()
    {
        // Arrange
        var row = Row(Column("Key", "Id"), Column("Compare", "Name"), Column("Write", "Id"), Column("Write", "Name", 1));
        row.EffectiveFromColumn = "ValidFrom";
        row.EffectiveToColumn = "ValidTo";
        row.CurrentFlagColumn = "IsCurrent";
        row.InsertOnlyRuleColumn = "Source";
        row.InsertOnlyRuleValue = "manual";

        // Act
        var command = Built("Upsert", Input(row))[0].ShouldBeOfType<BulkUpsertCommand<IDictionary<string, object?>>>();

        // Assert
        var versioning = command.Options.Versioning.ShouldNotBeNull();
        versioning.EffectiveFromColumn.ShouldBe("ValidFrom");
        versioning.EffectiveToColumn.ShouldBe("ValidTo");
        versioning.CurrentFlagColumn.ShouldBe("IsCurrent");
        versioning.InsertOnlyRule.ShouldNotBeNull().Column.ShouldBe("Source");
        versioning.InsertOnlyRule!.Value.ShouldBe("manual");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void UpsertPassesValueSourcesToTheCommandSoTheDatabaseSuppliesThem()
    {
        // Arrange
        var row = Row(Column("Key", "Id"), Column("Write", "Id"), Column("Write", "Name", 1));
        row.ValueSources = [Sequence("Number"), Stamp("LoadedAt")];

        // Act
        var command = Built("Upsert", Input(row))[0].ShouldBeOfType<BulkUpsertCommand<IDictionary<string, object?>>>();

        // Assert
        command.Options.ValueSources!.Select(b => b.Column).ShouldBe(new[] { "Number", "LoadedAt" });
        command.Options.ValueSources![0].Source.ShouldBeOfType<NextSequenceValueSource>();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void UpsertRefusesWhatTheCommandLayerRefusesWithItsReason()
    {
        // Arrange: the key is not among the write columns
        var row = Row(Column("Key", "Id"), Column("Write", "Name"));

        // Act / Assert
        Refused("Upsert", Input(row), "PIPELINERUN-20224", "key and compare column 'Id' is not in the write columns");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void UpsertWithAnAbsentFromSourceSoftDeleteFollowsTheUpsertWithAFlaggingDelete()
    {
        // Arrange
        var row = Row(Column("Key", "Id"), Column("Write", "Id"), Column("Write", "Name", 1));
        row.SoftDeleteMode = "AbsentFromSource";
        row.DeletedFlagColumn = "IsDeleted";
        row.DeletedAtColumn = "DeletedAt";

        // Act
        var commands = Built("Upsert", Input(row, Rows(3)));

        // Assert
        commands.Count.ShouldBe(2);
        commands[0].ShouldBeOfType<BulkUpsertCommand<IDictionary<string, object?>>>();
        var delete = commands[1].ShouldBeOfType<DeleteCommand>();
        delete.Filter.ShouldBeNull();
        var soft = delete.SoftDelete.ShouldNotBeNull();
        soft.DeletedFlagColumn.ShouldBe("IsDeleted");
        soft.DeletedAtColumn.ShouldBe("DeletedAt");
        ReferenceEquals(soft.Mode, CommandSoftDeleteModes.AbsentFromSource).ShouldBeTrue();
        soft.AbsentFromSource!.KeyColumns.ShouldBe(new[] { "Id" });
        soft.AbsentFromSource.Rows.Select(r => r["Id"]).ShouldBe(new object?[] { 1, 2, 3 });
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void UpsertRefusesAMarkerSoftDeleteBecauseItTakesASourceSetToCompare()
    {
        // Arrange
        var row = Row(Column("Key", "Id"), Column("Write", "Id"), Column("Write", "Name", 1));
        row.SoftDeleteMode = "MarkerMatches";
        row.DeletedFlagColumn = "IsDeleted";
        row.SoftDeleteMarkerColumn = "Status";
        row.SoftDeleteMarkerValue = "closed";

        // Act / Assert
        Refused("Upsert", Input(row), "PIPELINERUN-20223", "MarkerMatches");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void UpsertRefusesAnAbsentSetSoftDeleteWhenTheSourceProducedNoRows()
    {
        // Arrange: flagging every key absent from an empty source would flag the whole destination
        var row = Row(Column("Key", "Id"), Column("Write", "Id"), Column("Write", "Name", 1));
        row.SoftDeleteMode = "AbsentFromSource";
        row.DeletedFlagColumn = "IsDeleted";

        // Act / Assert
        Refused("Upsert", Input(row, []), "PIPELINERUN-20228");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void UpsertOfNoRowsBuildsNothing()
    {
        var row = Row(Column("Key", "Id"), Column("Write", "Id"));

        Built("Upsert", Input(row, [])).ShouldBeEmpty();
    }

    // ---- Update

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void UpdateBuildsOneUpdatePerRowMatchedOnTheKeyWritingOnlyTheWriteColumns()
    {
        // Arrange
        var row = Row(Column("Key", "Id"), Column("Write", "Name"));
        row.ValueSources = [Stamp("ModifiedAt")];

        // Act
        var commands = Built("Update", Input(row, Rows(2)));

        // Assert
        commands.Count.ShouldBe(2);
        var second = commands[1].ShouldBeOfType<UpdateCommand<IDictionary<string, object?>>>();
        second.Data.Keys.OrderBy(k => k, StringComparer.Ordinal).ShouldBe(new[] { "ModifiedAt", "Name" });
        second.Data["Name"].ShouldBe("n2");
        var condition = second.Filter!.Root.ShouldBeOfType<FilterCondition>();
        condition.PropertyName.ShouldBe("Id");
        condition.Operator.Name.ShouldBe("Equal");
        condition.Value.ShouldBe(2);
        second.SoftDelete.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void UpdateMatchesACompositeKeyOnEveryKeyColumn()
    {
        // Arrange
        var row = Row(Column("Key", "Id"), Column("Key", "Status", 1), Column("Write", "Name"));

        // Act
        var command = Built("Update", Input(row, Rows(1)))[0].ShouldBeOfType<UpdateCommand<IDictionary<string, object?>>>();

        // Assert
        var group = command.Filter!.Root.ShouldBeOfType<FilterGroup>();
        group.Operator.Name.ShouldBe("And");
        group.Nodes.Cast<FilterCondition>().Select(c => c.PropertyName).ShouldBe(new[] { "Id", "Status" });
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void UpdateWithAMarkerSoftDeleteCarriesTheSoftDeleteOnEveryUpdate()
    {
        // Arrange
        var row = Row(Column("Key", "Id"), Column("Write", "Name"));
        row.SoftDeleteMode = "MarkerMatches";
        row.DeletedFlagColumn = "IsDeleted";
        row.SoftDeleteMarkerColumn = "Status";
        row.SoftDeleteMarkerValue = "closed";

        // Act
        var commands = Built("Update", Input(row, Rows(2)));

        // Assert
        commands.Cast<UpdateCommand<IDictionary<string, object?>>>().ShouldAllBe(c => c.SoftDelete != null);
        var soft = commands.Cast<UpdateCommand<IDictionary<string, object?>>>().First().SoftDelete!;
        soft.Marker!.Column.ShouldBe("Status");
        soft.Marker.Value.ShouldBe("closed");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void UpdateRefusesAnAbsentFromSourceSoftDelete()
    {
        // Arrange
        var row = Row(Column("Key", "Id"), Column("Write", "Name"));
        row.SoftDeleteMode = "AbsentFromSource";
        row.DeletedFlagColumn = "IsDeleted";

        // Act / Assert
        Refused("Update", Input(row), "PIPELINERUN-20223", "AbsentFromSource");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void UpdateRefusesARowWithNoValueForTheKey()
    {
        // Arrange
        var rows = Rows(1);
        rows[0]["Id"] = null;

        // Act / Assert
        Refused("Update", Input(Row(Column("Key", "Id"), Column("Write", "Name")), rows), "PIPELINERUN-20227", "Id");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void UpdateRefusesASequenceValueSource()
    {
        // Arrange
        var row = Row(Column("Key", "Id"), Column("Write", "Name"));
        row.ValueSources = [Sequence("Number")];

        // Act / Assert
        Refused("Update", Input(row), "PIPELINERUN-20213", "Update");
    }

    // ---- Delete

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void DeleteBuildsOneHardDeletePerRowMatchedOnTheKey()
    {
        // Arrange
        var row = Row(Column("Key", "Id"));

        // Act
        var commands = Built("Delete", Input(row, Rows(2)));

        // Assert
        commands.Count.ShouldBe(2);
        var first = commands[0].ShouldBeOfType<DeleteCommand>();
        first.SoftDelete.ShouldBeNull();
        first.Filter!.Root.ShouldBeOfType<FilterCondition>().Value.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void DeleteWithAMarkerSoftDeleteFlagsTheMatchedRowsInsteadOfRemovingThem()
    {
        // Arrange
        var row = Row(Column("Key", "Id"));
        row.SoftDeleteMode = "MarkerMatches";
        row.DeletedFlagColumn = "IsDeleted";
        row.SoftDeleteMarkerColumn = "Status";
        row.SoftDeleteMarkerValue = "open";

        // Act
        var commands = Built("Delete", Input(row, Rows(2)));

        // Assert
        commands.Count.ShouldBe(2);
        commands.Cast<DeleteCommand>().ShouldAllBe(c => c.SoftDelete != null && c.Filter != null);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void DeleteWithAnAbsentFromSourceSoftDeleteIsOneSetBasedCommandOverTheOutputKeys()
    {
        // Arrange
        var row = Row(Column("Key", "Id"));
        row.SoftDeleteMode = "AbsentFromSource";
        row.DeletedFlagColumn = "IsDeleted";

        // Act
        var commands = Built("Delete", Input(row, Rows(3)));

        // Assert
        commands.Count.ShouldBe(1);
        var delete = commands[0].ShouldBeOfType<DeleteCommand>();
        delete.Filter.ShouldBeNull();
        delete.SoftDelete!.AbsentFromSource!.Rows.Count().ShouldBe(3);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void DeleteRefusesAnAbsentSetSoftDeleteWhenTheSourceProducedNoRows()
    {
        // Arrange
        var row = Row(Column("Key", "Id"));
        row.SoftDeleteMode = "AbsentFromSource";
        row.DeletedFlagColumn = "IsDeleted";

        // Act / Assert
        Refused("Delete", Input(row, []), "PIPELINERUN-20228");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void DeleteOfNoRowsBuildsNothing()
    {
        Built("Delete", Input(Row(Column("Key", "Id")), [])).ShouldBeEmpty();
    }

    // ---- Truncate

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void TruncateBuildsOneUnrestrictedDeleteWhateverTheRows()
    {
        var command = Built("Truncate", Input(Row(), [])).Single().ShouldBeOfType<DeleteCommand>();

        command.Filter.ShouldBeNull();
        command.SoftDelete.ShouldBeNull();
    }
}
