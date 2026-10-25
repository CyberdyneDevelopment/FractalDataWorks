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
}
