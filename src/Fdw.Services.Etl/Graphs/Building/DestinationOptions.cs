using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.OptionTypes;
using Fdw.Services.Etl.Pipelines.Loads;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Graphs.Building;

/// <summary>
/// The load options of a destination component (FDW-1636): the load row the executor writes from, and the check of those options.
/// </summary>
/// <remarks>
/// Why a load row: <see cref="PipelineLoadExecutor"/> and <see cref="PipelineLoadValidator"/> hold the rules for columns, versioning,
/// soft delete and column values, and they work on this row. A destination fills the row from its own settings and goes through them,
/// so a destination writes and is judged exactly as the load step it replaces was.
/// </remarks>
internal static class DestinationOptions
{
    private const string ValueSourceOrderRole = "SequenceOrder";

    private static readonly string[] ColumnSettings = ["keyColumns", "compareColumns", "writeColumns"];

    /// <summary>Fills the load row a destination component writes from.</summary>
    /// <param name="component">The destination.</param>
    /// <param name="kind">The load kind it states.</param>
    /// <returns>The row, with the columns, versioning, soft delete and value sources the component states.</returns>
    internal static PipelineLoadStepConfiguration Row(PipelineComponentConfiguration component, ILoadStepKind kind)
    {
        var row = ColumnsRow(component, kind);
        row.EffectiveFromColumn = component.EffectiveFromColumn;
        row.EffectiveToColumn = component.EffectiveToColumn;
        row.CurrentFlagColumn = component.CurrentFlagColumn;
        row.InsertOnlyRuleColumn = component.InsertOnlyRuleColumn;
        row.InsertOnlyRuleValue = component.InsertOnlyRuleValue;
        row.SoftDeleteMode = component.SoftDeleteMode;
        row.DeletedFlagColumn = component.DeletedFlagColumn;
        row.DeletedAtColumn = component.DeletedAtColumn;
        row.SoftDeleteMarkerColumn = component.SoftDeleteMarkerColumn;
        row.SoftDeleteMarkerValue = component.SoftDeleteMarkerValue;
        row.ValueSources = [.. component.ValueSources.OrderBy(source => source.Ordinal).Select(ValueSource)];
        return row;
    }

    /// <summary>Fills the load row with the destination's columns alone, for the truncate that empties the target before the first batch.</summary>
    /// <param name="component">The destination.</param>
    /// <param name="kind">The load kind the row is for.</param>
    /// <returns>The row, with the key, compare and write columns.</returns>
    internal static PipelineLoadStepConfiguration ColumnsRow(PipelineComponentConfiguration component, ILoadStepKind kind)
    {
        var row = new PipelineLoadStepConfiguration
        {
            Name = component.Name,
            Phase = "Load",
            Kind = kind.Name,
            IsEnabled = true,
        };
        AddColumns(row, component, "keyColumns", "Key");
        AddColumns(row, component, "compareColumns", "Compare");
        AddColumns(row, component, "writeColumns", "Write");
        return row;
    }

    /// <summary>Checks the load options of a destination with the rules the load validator holds.</summary>
    /// <param name="component">The component; one that states no load kind is not a destination and has nothing to check here.</param>
    /// <param name="logger">The logger the load validator reports to.</param>
    /// <returns>A problem for each option group the validator refuses, placed on the setting that starts the group.</returns>
    internal static IReadOnlyList<GraphProblem> Problems(PipelineComponentConfiguration component, ILogger logger)
    {
        // Why nothing for a missing or unknown load kind: the settings check and the builder report it.
        if (component.LoadKind is null)
        {
            return [];
        }

        var kind = LoadStepKinds.ByName(component.LoadKind);
        if (ReferenceEquals(kind, LoadStepKinds.NotFound))
        {
            return [];
        }

        var row = Row(component, kind);
        var owner = string.Concat("graph destination '", component.Name, "'");
        var checks = new (string? Setting, IGenericResult Result)[]
        {
            (null, ColumnListsBound(component) ? GenericResult.Success() : PipelineLoadValidator.ValidateColumnLists(owner, row, kind, kind.Name, logger)),
            ("effectiveFromColumn", PipelineLoadValidator.ValidateVersioning(owner, row, kind, kind.Name, logger)),
            ("softDeleteMode", PipelineLoadValidator.ValidateSoftDelete(owner, row, kind, kind.Name, logger)),
            ("valueSources", PipelineLoadValidator.ValidateValueSources(owner, row, hasSourceDataSet: false, forContainer: false, logger)),
        };
        return [.. checks.Where(check => check.Result.IsFailure).Select(check => Problem(component, check.Setting, check.Result))];
    }

    // Why the column check waits: a column list a run supplies (FDW-1745) is not known when the graph is saved, and the load validator judges the lists whole.
    // The run builds the destination from the lists it was given, and the same validator judges them then.
    internal static bool ColumnListsBound(PipelineComponentConfiguration component) =>
        component.SettingBindings.Any(binding => ColumnSettings.Contains(binding.SettingName, StringComparer.OrdinalIgnoreCase));

    /// <summary>Judges the key, compare and write columns of a destination whole, with the lists a run supplied (FDW-1745).</summary>
    /// <param name="component">The destination, as the run builds it: its bound lists already hold the run's columns.</param>
    /// <param name="kind">The load kind it states.</param>
    /// <param name="logger">The logger the load validator reports to.</param>
    /// <returns>Success, or the refusal that names what is wrong with the lists.</returns>
    internal static IGenericResult ColumnLists(PipelineComponentConfiguration component, ILoadStepKind kind, ILogger logger) =>
        PipelineLoadValidator.ValidateColumnLists(string.Concat("graph destination '", component.Name, "'"), Row(component, kind), kind, kind.Name, logger);

    private static GraphProblem Problem(PipelineComponentConfiguration component, string? setting, IGenericResult refused) =>
        new(
            setting is null ? ["components", component.Name, "settings"] : ["components", component.Name, "settings", setting],
            refused.CurrentMessage is { Length: > 0 } reason
                ? reason
                : $"the load options of '{component.Name}' were refused without a reason; see the server log");

    private static PipelineLoadValueSourceConfiguration ValueSource(PipelineComponentValueSourceConfiguration source) => new()
    {
        Name = source.ColumnName,
        ColumnName = source.ColumnName,
        SourceKind = source.SourceKind,
        SourceField = source.SourceField,
        SequenceName = source.SequenceName,
        SequenceSchema = source.SequenceSchema,
        SequenceDatabase = source.SequenceDatabase,
        ConstantValue = source.ConstantValue,
        ConstantDataType = source.ConstantDataType,
        SequenceOrderColumns =
        [
            .. source.OrderColumns.OrderBy(column => column.Ordinal).Select(column => new PipelineLoadColumnConfiguration
            {
                Name = column.ColumnName, Role = ValueSourceOrderRole, ColumnName = column.ColumnName, Ordinal = column.Ordinal,
            }),
        ],
    };

    private static void AddColumns(PipelineLoadStepConfiguration row, PipelineComponentConfiguration component, string setting, string role)
    {
        foreach (var column in component.Columns
            .Where(c => string.Equals(c.SettingName, setting, StringComparison.OrdinalIgnoreCase))
            .OrderBy(c => c.Ordinal))
        {
            row.Columns.Add(new PipelineLoadColumnConfiguration
            {
                Name = string.Concat(role, ":", column.ColumnName),
                Role = role,
                ColumnName = column.ColumnName,
                Ordinal = column.Ordinal,
            });
        }
    }
}
