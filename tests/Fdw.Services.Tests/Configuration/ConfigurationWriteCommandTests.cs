using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Collections.Attributes;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Commands.Data.SoftDeletes;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Tests.Configuration;

/// <summary>
/// What a configuration save and retire do: the steps each expands into for a container, the rows each step reads or
/// flags, and the result assembled from what the steps answer.
/// </summary>
[Collection(nameof(ServicesTestCollection))]
public sealed class ConfigurationWriteCommandTests
{
    private static readonly Guid RowId = Guid.Parse("11111111-0000-0000-0000-000000000001");
    private static readonly Guid OwnerId = Guid.Parse("22222222-0000-0000-0000-000000000002");

    [GenerateMapper]
    public sealed class SettingRow
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Value { get; set; }
    }

    private static IField Field(string name, bool nullable = false, bool systemProvided = false)
    {
        var field = new Mock<IField>();
        field.Setup(f => f.Name).Returns(name);
        field.Setup(f => f.IsNullable).Returns(nullable);
        field.Setup(f => f.IsSystemProvided).Returns(systemProvided);
        return field.Object;
    }

    private static IDataField DataField(string name)
    {
        var field = new Mock<IDataField>();
        field.Setup(f => f.Name).Returns(name);
        return field.Object;
    }

    private static IContainerKey Key(string name, string type, params string[] fields)
    {
        var key = new Mock<IContainerKey>();
        key.Setup(k => k.KeyName).Returns(name);
        key.Setup(k => k.KeyType).Returns((KeyTypeBase)KeyTypes.ByName(type));
        key.Setup(k => k.KeyFields).Returns(fields.Select(f =>
        {
            var keyField = new Mock<IContainerKeyField>();
            keyField.Setup(k => k.LocalField).Returns(DataField(f));
            return keyField.Object;
        }).ToList());
        return key.Object;
    }

    // A container of Id, OwnerId, Name, a nullable Value, a NOT NULL Note, and the three versioning columns the database provides.
    private static IDataContainer Container(IReadOnlyList<ReferencingKeyBinding>? referencing = null, bool logicalKey = true)
    {
        var schema = new Mock<IContainerSchema>();
        schema.Setup(s => s.Fields).Returns(new[]
        {
            Field("Id"), Field("OwnerId", nullable: true), Field("Name"), Field("Value", nullable: true), Field("Note"),
            Field("IsCurrent", systemProvided: true), Field("IsDeleted", systemProvided: true), Field("ModifyDate", systemProvided: true),
        });

        var container = new Mock<IDataContainer>();
        container.Setup(c => c.Name).Returns("Setting");
        container.Setup(c => c.Schema).Returns(schema.Object);
        container.Setup(c => c.Keys).Returns(logicalKey ? new List<IContainerKey> { Key("PK_Setting_Logical", "Logical", "Id") } : new List<IContainerKey>());
        container.Setup(c => c.ReferencingKeys).Returns(GenericResult<IReadOnlyList<ReferencingKeyBinding>>.Success(referencing ?? []));
        return container.Object;
    }

    private static IReadOnlyList<ReferencingKeyBinding> ReferencedBy(string referencedField)
    {
        var keyField = new Mock<IContainerKeyField>();
        keyField.Setup(k => k.ReferencedField).Returns(DataField(referencedField));
        var key = new Mock<IContainerKey>();
        key.Setup(k => k.KeyName).Returns("FK_Child_Setting");
        key.Setup(k => k.KeyFields).Returns(new List<IContainerKeyField> { keyField.Object });
        var owner = new Mock<IDataContainer>();
        owner.Setup(c => c.Name).Returns("SettingChild");
        return [new ReferencingKeyBinding(key.Object, owner.Object)];
    }

    private static ConfigurationSaveCommand<SettingRow> Save(bool replaces, IReadOnlyDictionary<string, object?>? fieldValues = null, Guid? id = null)
        => new(new SettingRow { Id = id ?? RowId, Name = "alpha", Value = "one" }, fieldValues ?? new Dictionary<string, object?>(), replaces);

    private static IReadOnlyList<(string Field, object? Value)> Conditions(IFilterExpression? filter)
        => filter!.Root.ShouldBeOfType<FilterGroup>().Nodes.Cast<FilterCondition>().Select(c => (c.PropertyName, c.Value)).ToList();

    private static IReadOnlyList<ExpandedCommand> Steps(IGenericResult<IReadOnlyList<ExpandedCommand>> expansion)
    {
        expansion.IsSuccess.ShouldBeTrue(expansion.CurrentMessage);
        return expansion.Value!;
    }

    private static IReadOnlyDictionary<string, object?> Row(string field, object value) => new Dictionary<string, object?> { [field] = value };

    // ── Save ─────────────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ASaveThatReplacesReadsTheCurrentRowClosesItAndInsertsTheNewVersion()
    {
        var steps = Steps(Save(replaces: true).Expand(Container()));

        steps.Count.ShouldBe(3);
        var read = steps[0].Command.ShouldBeOfType<QueryCommand<Dictionary<string, object?>>>();
        steps[0].AnswersRows.ShouldBeTrue();
        steps[0].CountsAsAffected.ShouldBeFalse();
        read.ReadsForWrite.ShouldBeTrue();
        Conditions(read.Filter).ShouldBe([("Id", (object)RowId), ("IsCurrent", true)]);

        var close = steps[1].Command.ShouldBeOfType<DeleteCommand>();
        close.SoftDelete.ShouldNotBeNull().DeletedFlagColumn.ShouldBe("IsCurrent");
        close.SoftDelete.FlagValue.ShouldBeFalse();
        close.SoftDelete.DeletedAtColumn.ShouldBe("ModifyDate");
        ReferenceEquals(close.SoftDelete.Mode, SoftDeleteModes.FilterMatches).ShouldBeTrue();
        Conditions(close.Filter).ShouldBe([("Id", (object)RowId), ("IsCurrent", true)]);

        steps[2].Command.ShouldBeOfType<InsertCommand<Dictionary<string, object?>>>();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ASaveThatReplacesNothingIsOnlyTheInsert()
    {
        var steps = Steps(Save(replaces: false).Expand(Container()));

        steps.Count.ShouldBe(1);
        steps[0].Command.ShouldBeOfType<InsertCommand<Dictionary<string, object?>>>();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void TheInsertWritesTheDeclaredWritableFieldsTheRecordHasAndTheNewVersionAsCurrentAndNotDeleted()
    {
        var insert = Steps(Save(replaces: false).Expand(Container()))[0].Command.ShouldBeOfType<InsertCommand<Dictionary<string, object?>>>();

        var row = insert.Data.ShouldHaveSingleItem();
        row.Keys.OrderBy(k => k, StringComparer.Ordinal).ShouldBe(["Id", "IsCurrent", "IsDeleted", "Name", "Value"]);
        row["Id"].ShouldBe(RowId);
        row["Name"].ShouldBe("alpha");
        row["IsCurrent"].ShouldBe(true);
        row["IsDeleted"].ShouldBe(false);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AContainerThatDoesNotDeclareAVersionColumnIsRefusedByName()
    {
        var container = Container();
        Mock.Get(container.Schema).Setup(s => s.Fields).Returns(new[] { Field("Id"), Field("Name"), Field("Value", nullable: true), Field("IsCurrent", systemProvided: true) });

        var expansion = Save(replaces: false).Expand(container);

        expansion.IsFailure.ShouldBeTrue();
        expansion.CurrentMessage.ShouldNotBeNull().ShouldContain("IsDeleted");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AFieldValueTheRecordDoesNotCarryIsWrittenWithTheRow()
    {
        var steps = Steps(Save(replaces: false, Row("OwnerId", OwnerId)).Expand(Container()));

        steps[0].Command.ShouldBeOfType<InsertCommand<Dictionary<string, object?>>>().Data.Single()["OwnerId"].ShouldBe(OwnerId);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ANullValueInANotNullFieldIsLeftOutSoItsColumnDefaultApplies()
    {
        var steps = Steps(Save(replaces: false, new Dictionary<string, object?> { ["Note"] = null, ["OwnerId"] = null }).Expand(Container()));

        var row = steps[0].Command.ShouldBeOfType<InsertCommand<Dictionary<string, object?>>>().Data.Single();
        row.ContainsKey("Note").ShouldBeFalse();
        row.ContainsKey("OwnerId").ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void TheOwnersKeyValuesAreMatchedOnAlongsideTheLogicalKey()
    {
        var steps = Steps(Save(replaces: true, Row("OwnerId", OwnerId)).Expand(Container()));

        Conditions(steps[1].Command.ShouldBeOfType<DeleteCommand>().Filter).ShouldBe([("OwnerId", (object)OwnerId), ("Id", RowId), ("IsCurrent", true)]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void TheReferencedKeyColumnsAreReadAndReturnedWhenAChildReferencesTheContainer()
    {
        var steps = Steps(Save(replaces: true).Expand(Container(ReferencedBy("Id"))));

        var read = steps[0].Command.ShouldBeOfType<QueryCommand<Dictionary<string, object?>>>();
        read.Projection.ShouldNotBeNull().PropertyNames.ShouldBe(["Id"]);
        var insert = steps[2].Command.ShouldBeOfType<InsertCommand<Dictionary<string, object?>>>();
        insert.ReturnColumns.ShouldBe(["Id"]);
        steps[2].AnswersRows.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void WhenNothingReferencesTheContainerTheInsertAnswersACountAndTheReadAnswersTheMatchColumns()
    {
        var steps = Steps(Save(replaces: true).Expand(Container()));

        steps[2].AnswersRows.ShouldBeFalse();
        steps[2].Command.ShouldBeOfType<InsertCommand<Dictionary<string, object?>>>().ReturnColumns.ShouldBeNull();
        steps[0].Command.ShouldBeOfType<QueryCommand<Dictionary<string, object?>>>().Projection.ShouldNotBeNull().PropertyNames.ShouldBe(["Id"]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AForeignKeyWhoseReferencedFieldIsNotResolvedIsRefusedByName()
    {
        var keyField = new Mock<IContainerKeyField>();
        keyField.Setup(k => k.ReferencedField).Returns((IDataField?)null);
        var key = new Mock<IContainerKey>();
        key.Setup(k => k.KeyName).Returns("FK_Child_Setting");
        key.Setup(k => k.KeyFields).Returns(new List<IContainerKeyField> { keyField.Object });
        var owner = new Mock<IDataContainer>();
        owner.Setup(c => c.Name).Returns("SettingChild");

        var expansion = Save(replaces: true).Expand(Container([new ReferencingKeyBinding(key.Object, owner.Object)]));

        expansion.IsFailure.ShouldBeTrue();
        expansion.CurrentMessage.ShouldNotBeNull().ShouldContain("FK_Child_Setting");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ASaveWithNoValueForAFieldOfTheLogicalKeyIsRefused()
    {
        var save = new ConfigurationSaveCommand<SettingRow>(new SettingRow { Name = "alpha" }, replacesCurrent: true);
        var container = Container();
        Mock.Get(container).Setup(c => c.Keys).Returns(new List<IContainerKey> { Key("PK_Setting_Logical", "Logical", "Note") });

        var expansion = save.Expand(container);

        expansion.IsFailure.ShouldBeTrue();
        expansion.CurrentMessage.ShouldNotBeNull().ShouldContain("Note");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ASaveWithNeitherOwnerKeyValuesNorALogicalKeyIsRefusedBecauseNothingSaysWhichRowItReplaces()
    {
        var expansion = Save(replaces: true).Expand(Container(logicalKey: false));

        expansion.IsFailure.ShouldBeTrue();
        expansion.CurrentMessage.ShouldNotBeNull().ShouldContain("Logical key");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AContainerThatIsNotAKeyedDataContainerIsRefused()
    {
        var container = new Mock<IStorageContainer>();
        container.Setup(c => c.Name).Returns("Setting");

        Save(replaces: true).Expand(container.Object).IsFailure.ShouldBeTrue();
    }

    // ── Retire ───────────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ADeleteReadsTheCurrentRowsNotAlreadyDeletedAndMarksThemDeletedLeavingThemCurrent()
    {
        var steps = Steps(new ConfigurationRetireCommand(Row("Id", RowId), markDeleted: true).Expand(Container()));

        steps.Count.ShouldBe(2);
        Conditions(steps[0].Command.ShouldBeOfType<QueryCommand<Dictionary<string, object?>>>().Filter)
            .ShouldBe([("Id", (object)RowId), ("IsCurrent", true), ("IsDeleted", false)]);
        var flag = steps[1].Command.ShouldBeOfType<DeleteCommand>().SoftDelete.ShouldNotBeNull();
        flag.DeletedFlagColumn.ShouldBe("IsDeleted");
        flag.FlagValue.ShouldBeTrue();
        flag.DeletedAtColumn.ShouldBe("ModifyDate");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ADeleteFlagsOnlyTheRowsItReadSoARowAlreadyDeletedKeepsItsModifyDate()
    {
        var steps = Steps(new ConfigurationRetireCommand(Row("Id", RowId), markDeleted: true).Expand(Container()));

        Conditions(steps[1].Command.ShouldBeOfType<DeleteCommand>().Filter)
            .ShouldBe([("Id", (object)RowId), ("IsCurrent", true), ("IsDeleted", false)]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ASupersedeReadsEveryCurrentRowAndClosesThemWithoutMarkingThemDeleted()
    {
        var steps = Steps(new ConfigurationRetireCommand(Row("Id", RowId), markDeleted: false).Expand(Container()));

        Conditions(steps[0].Command.ShouldBeOfType<QueryCommand<Dictionary<string, object?>>>().Filter).ShouldBe([("Id", (object)RowId), ("IsCurrent", true)]);
        var flag = steps[1].Command.ShouldBeOfType<DeleteCommand>().SoftDelete.ShouldNotBeNull();
        flag.DeletedFlagColumn.ShouldBe("IsCurrent");
        flag.FlagValue.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ASupersedeClosesADeletedCurrentRowToo()
    {
        var steps = Steps(new ConfigurationRetireCommand(Row("Id", RowId), markDeleted: false).Expand(Container()));

        Conditions(steps[1].Command.ShouldBeOfType<DeleteCommand>().Filter).ShouldBe([("Id", (object)RowId), ("IsCurrent", true)]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ARetireByAKeyMatchesOnTheFieldsOfTheKeyTheContainerDeclares()
    {
        var steps = Steps(new ConfigurationRetireCommand((KeyTypeBase)KeyTypes.ByName("Logical"), [RowId], markDeleted: true).Expand(Container()));

        Conditions(steps[1].Command.ShouldBeOfType<DeleteCommand>().Filter).ShouldBe([("Id", (object)RowId), ("IsCurrent", true), ("IsDeleted", false)]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ARetireByAKeyTheContainerDoesNotDeclareIsRefused()
    {
        var expansion = new ConfigurationRetireCommand((KeyTypeBase)KeyTypes.ByName("Physical"), [1], markDeleted: true).Expand(Container());

        expansion.IsFailure.ShouldBeTrue();
        expansion.CurrentMessage.ShouldNotBeNull().ShouldContain("Physical");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ARetireWithTheWrongNumberOfKeyValuesIsRefused()
    {
        new ConfigurationRetireCommand((KeyTypeBase)KeyTypes.ByName("Logical"), [RowId, OwnerId], markDeleted: true)
            .Expand(Container()).IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ARetireOnAFieldTheContainerDoesNotDeclareIsRefused()
    {
        var expansion = new ConfigurationRetireCommand(Row("NoSuchField", 1), markDeleted: true).Expand(Container());

        expansion.IsFailure.ShouldBeTrue();
        expansion.CurrentMessage.ShouldNotBeNull().ShouldContain("NoSuchField");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ARetireThatMatchesOnNothingIsRefusedRatherThanRetiringEveryCurrentRow()
    {
        new ConfigurationRetireCommand(new Dictionary<string, object?>(), markDeleted: true).Expand(Container()).IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ARetireWithNoValueForAMatchFieldIsRefused()
    {
        var expansion = new ConfigurationRetireCommand(new Dictionary<string, object?> { ["Id"] = null }, markDeleted: true).Expand(Container());

        expansion.IsFailure.ShouldBeTrue();
        expansion.CurrentMessage.ShouldNotBeNull().ShouldContain("Id");
    }

    // ── Assemble ─────────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ASaveAnswersTheKeysOfTheRowsItRetiredAndOfTheRowItWrote()
    {
        var result = Save(replaces: true).Assemble([
            new ExpansionAnswer(0, [Row("Id", 1)]),
            new ExpansionAnswer(1),
            new ExpansionAnswer(0, [Row("Id", 2)]),
        ]);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        result.Value!.Retired.ShouldHaveSingleItem()["Id"].ShouldBe(1);
        result.Value.Written.ShouldNotBeNull()["Id"].ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ASaveThatReplacesNothingRetiredNothing()
    {
        var result = Save(replaces: false).Assemble([new ExpansionAnswer(0, [Row("Id", 2)])]);

        result.Value!.Retired.ShouldBeEmpty();
        result.Value.Written.ShouldNotBeNull()["Id"].ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void WhenTheInsertAnswersACountTheWrittenKeyIsEmpty()
    {
        var result = Save(replaces: false).Assemble([new ExpansionAnswer(1)]);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        result.Value!.Written.ShouldNotBeNull().ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AnInsertThatAnswersNoRowFailsTheSaveBecauseTheWrittenKeyIsUnknown()
    {
        Save(replaces: false).Assemble([new ExpansionAnswer(0, [])]).IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AStepAnswerThatIsMissingFailsTheSave()
    {
        Save(replaces: true).Assemble([new ExpansionAnswer(1), new ExpansionAnswer(1), new ExpansionAnswer(1)]).IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ARetireAnswersTheKeysOfTheRowsItReadAndNoWrittenKey()
    {
        var result = new ConfigurationRetireCommand(Row("Id", RowId), markDeleted: true).Assemble([new ExpansionAnswer(0, [Row("Id", 1), Row("Id", 3)]), new ExpansionAnswer(2)]);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        result.Value!.Retired.Count.ShouldBe(2);
        result.Value.Written.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ARetireWithoutTheRowsItReadFails()
    {
        new ConfigurationRetireCommand(Row("Id", RowId), markDeleted: true).Assemble([new ExpansionAnswer(1), new ExpansionAnswer(1)]).IsFailure.ShouldBeTrue();
    }
}
