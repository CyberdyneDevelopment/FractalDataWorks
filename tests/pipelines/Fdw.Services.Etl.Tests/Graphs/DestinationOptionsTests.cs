using System.Linq;
using Fdw.Services.Etl.Abstractions.OptionTypes;
using Fdw.Services.Etl.Graphs;
using Fdw.Services.Etl.Graphs.Building;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Tests.Graphs;

/// <summary>
/// Tests for the load options of a destination (FDW-1636): they fill the load row the executor writes from, and the load validator
/// judges them, so a destination is held to what the load step it replaces was.
/// </summary>
public sealed class DestinationOptionsTests
{
    private static PipelineComponentConfiguration Upsert()
    {
        var destination = new PipelineComponentConfiguration
        {
            Name = "load", ComponentType = "DataSetDestination", DataSetName = "Orders", LoadKind = "Upsert", TruncateFirst = false,
        };
        AddColumns(destination, "keyColumns", "Id");
        AddColumns(destination, "compareColumns", "Amount");
        AddColumns(destination, "writeColumns", "Id", "Amount");
        return destination;
    }

    // An absent-key soft delete into a database stages its keys (FDW-4956), so it states its stage.
    private static void Staged(PipelineComponentConfiguration destination)
    {
        destination.StageContainer = "OrdersStage";
        destination.StageKeyColumn = "StageKey";
        destination.SweepRejectedRowsRule = "Refuse";
    }

    private static void AddColumns(PipelineComponentConfiguration component, string setting, params string[] columns)
    {
        for (var ordinal = 0; ordinal < columns.Length; ordinal++)
        {
            component.Columns.Add(new PipelineComponentColumnConfiguration { SettingName = setting, Ordinal = ordinal, ColumnName = columns[ordinal] });
        }
    }

    private static string[] Paths(PipelineComponentConfiguration component) =>
        [.. DestinationOptions.Problems(component, NullLogger.Instance).Select(problem => string.Join('/', problem.Path))];

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void TheLoadRowCarriesTheVersioningSoftDeleteAndValueSourcesTheDestinationStates()
    {
        var destination = Upsert();
        destination.EffectiveFromColumn = "ValidFrom";
        destination.EffectiveToColumn = "ValidTo";
        destination.CurrentFlagColumn = "IsCurrent";
        destination.SoftDeleteMode = "AbsentFromSource";
        destination.DeletedFlagColumn = "IsGone";
        destination.DeletedFlagValue = true;
        Staged(destination);
        destination.ValueSources.Add(new PipelineComponentValueSourceConfiguration
        {
            Ordinal = 1, ColumnName = "Number", SourceKind = "NextSequenceValue", SequenceName = "OrderNumbers",
            OrderColumns = [new PipelineComponentValueSourceColumnConfiguration { Ordinal = 0, ColumnName = "Id" }],
        });
        destination.ValueSources.Add(new PipelineComponentValueSourceConfiguration
        {
            Ordinal = 0, ColumnName = "Origin", SourceKind = "Constant", ConstantValue = "orders", ConstantDataType = "String",
        });

        var row = DestinationOptions.Row(destination, LoadStepKinds.ByName("Upsert"));

        row.EffectiveFromColumn.ShouldBe("ValidFrom");
        row.EffectiveToColumn.ShouldBe("ValidTo");
        row.CurrentFlagColumn.ShouldBe("IsCurrent");
        row.SoftDeleteMode.ShouldBe("AbsentFromSource");
        row.DeletedFlagColumn.ShouldBe("IsGone");
        row.DeletedFlagValue.ShouldBe(true);
        row.StageContainer.ShouldBe("OrdersStage");
        row.StageKeyColumn.ShouldBe("StageKey");
        row.SweepRejectedRowsRule.ShouldBe("Refuse");
        row.ValueSources.Select(source => source.ColumnName).ShouldBe(["Origin", "Number"]);
        row.ValueSources[1].SequenceOrderColumns.Single().ColumnName.ShouldBe("Id");
        row.Columns.Count(column => column.Role == "Write").ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void TheTruncateRowCarriesTheColumnsAlone()
    {
        var destination = Upsert();
        destination.EffectiveFromColumn = "ValidFrom";
        destination.EffectiveToColumn = "ValidTo";

        var row = DestinationOptions.ColumnsRow(destination, LoadStepKinds.ByName("Truncate"));

        row.EffectiveFromColumn.ShouldBeNull();
        row.ValueSources.ShouldBeEmpty();
        row.Columns.Count.ShouldBe(4);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AVersionedUpsertWithItsOptionsInOrderHasNoProblem()
    {
        var destination = Upsert();
        destination.EffectiveFromColumn = "ValidFrom";
        destination.EffectiveToColumn = "ValidTo";
        destination.CurrentFlagColumn = "IsCurrent";
        destination.SoftDeleteMode = "AbsentFromSource";
        destination.DeletedFlagColumn = "IsGone";
        destination.DeletedFlagValue = true;
        Staged(destination);

        Paths(destination).ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void VersioningOnALoadKindThatDoesNotAllowItIsAProblemOnTheSetting()
    {
        var destination = Upsert();
        destination.LoadKind = "Insert";
        destination.Columns.Clear();
        AddColumns(destination, "writeColumns", "Id", "Amount");
        destination.EffectiveFromColumn = "ValidFrom";
        destination.EffectiveToColumn = "ValidTo";

        Paths(destination).ShouldBe(["components/load/settings/effectiveFromColumn"]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AVersionWithOnlyOneEndIsAProblemOnTheSetting()
    {
        var destination = Upsert();
        destination.EffectiveFromColumn = "ValidFrom";

        Paths(destination).ShouldBe(["components/load/settings/effectiveFromColumn"]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ASoftDeleteWithoutItsFlagColumnIsAProblemOnTheSetting()
    {
        var destination = Upsert();
        destination.SoftDeleteMode = "AbsentFromSource";
        Staged(destination);

        Paths(destination).ShouldBe(["components/load/settings/softDeleteMode"]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AnAbsentKeySoftDeleteIntoADatabaseThatNamesNoStageIsAProblemOnTheStageSetting()
    {
        var destination = Upsert();
        destination.SoftDeleteMode = "AbsentFromSource";
        destination.DeletedFlagColumn = "IsGone";
        destination.DeletedFlagValue = true;

        Paths(destination).ShouldBe(["components/load/settings/stageContainer"]);
        DestinationOptions.StagesKeys(destination).ShouldBeTrue();
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    [InlineData(null, "StageKey", "Refuse")]
    [InlineData("OrdersStage", null, "Refuse")]
    [InlineData("OrdersStage", "StageKey", null)]
    [InlineData("OrdersStage", "StageKey", "Sometimes")]
    public void AnAbsentKeySoftDeleteMissingAnyPartOfItsStageIsAProblem(string? stage, string? stageKey, string? rule)
    {
        var destination = Upsert();
        destination.SoftDeleteMode = "AbsentFromSource";
        destination.DeletedFlagColumn = "IsGone";
        destination.DeletedFlagValue = true;
        destination.StageContainer = stage;
        destination.StageKeyColumn = stageKey;
        destination.SweepRejectedRowsRule = rule;

        Paths(destination).ShouldBe(["components/load/settings/stageContainer"]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AMarkerSoftDeleteThatNamesAStageIsAProblem()
    {
        var destination = Upsert();
        destination.LoadKind = "Delete";
        destination.UnmatchedKeysRule = "Warn";
        destination.Columns.Clear();
        AddColumns(destination, "keyColumns", "Id");
        destination.SoftDeleteMode = "MarkerMatches";
        destination.DeletedFlagColumn = "IsGone";
        destination.DeletedFlagValue = true;
        destination.SoftDeleteMarkerColumn = "Status";
        destination.SoftDeleteMarkerValue = "closed";
        Staged(destination);

        Paths(destination).ShouldBe(["components/load/settings/stageContainer"]);
        DestinationOptions.StagesKeys(destination).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AnAbsentFromStageModeStatedByADestinationIsAProblem()
    {
        var destination = Upsert();
        destination.SoftDeleteMode = "AbsentFromStage";
        destination.DeletedFlagColumn = "IsGone";
        destination.DeletedFlagValue = true;

        Paths(destination).ShouldBe(["components/load/settings/softDeleteMode"]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AValueSourceThatLacksWhatItsKindNeedsIsAProblemOnTheSetting()
    {
        var destination = Upsert();
        destination.ValueSources.Add(new PipelineComponentValueSourceConfiguration { ColumnName = "Origin", SourceKind = "Constant", ConstantValue = "orders" });

        Paths(destination).ShouldBe(["components/load/settings/valueSources"]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AColumnValueSourceIsRefusedBecauseTheRowsComeFromTheFlowNotFromADataSetOfTheLoad()
    {
        var destination = Upsert();
        destination.ValueSources.Add(new PipelineComponentValueSourceConfiguration { ColumnName = "Origin", SourceKind = "Column", SourceField = "Region" });

        Paths(destination).ShouldBe(["components/load/settings/valueSources"]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AComponentThatStatesNoLoadKindHasNothingToCheck()
    {
        Paths(new PipelineComponentConfiguration { Name = "keep", ComponentType = "Filter" }).ShouldBeEmpty();
    }
    private static PipelineComponentConfiguration ByKey(string kind)
    {
        var destination = Upsert();
        destination.LoadKind = kind;
        destination.Columns.Clear();
        AddColumns(destination, "keyColumns", "Id");
        if (kind == "Update")
        {
            AddColumns(destination, "writeColumns", "Id", "Amount");
        }

        return destination;
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    [InlineData("Update")]
    [InlineData("Delete")]
    public void ALoadThatFindsRowsByKeyWithoutAnUnmatchedKeysRuleIsAProblemOnTheSetting(string kind)
    {
        Paths(ByKey(kind)).ShouldBe(["components/load/settings/unmatchedKeysRule"]);
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    [InlineData("Update", "Fail")]
    [InlineData("Delete", "Warn")]
    public void ALoadThatFindsRowsByKeyWithItsRuleHasNoProblem(string kind, string rule)
    {
        var destination = ByKey(kind);
        destination.UnmatchedKeysRule = rule;

        Paths(destination).ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AnUnknownUnmatchedKeysRuleIsAProblem()
    {
        var destination = ByKey("Update");
        destination.UnmatchedKeysRule = "Sometimes";

        Paths(destination).ShouldBe(["components/load/settings/unmatchedKeysRule"]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AnUpsertThatStatesAnUnmatchedKeysRuleIsAProblem()
    {
        var destination = Upsert();
        destination.UnmatchedKeysRule = "Warn";

        Paths(destination).ShouldBe(["components/load/settings/unmatchedKeysRule"]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AnAbsentFromSourceDeleteMatchesNothingByKeySoItTakesNoUnmatchedKeysRule()
    {
        var destination = ByKey("Delete");
        destination.SoftDeleteMode = "AbsentFromSource";
        destination.DeletedFlagColumn = "IsGone";
        destination.DeletedFlagValue = true;
        Staged(destination);

        Paths(destination).ShouldBeEmpty();
        destination.UnmatchedKeysRule = "Warn";
        Paths(destination).ShouldBe(["components/load/settings/unmatchedKeysRule"]);
    }
}
