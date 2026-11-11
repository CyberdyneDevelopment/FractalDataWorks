using System;
using System.Collections.Generic;
using System.Globalization;
using Fdw.Commands.Data.SoftDeletes;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.OptionTypes;
using Fdw.Services.Etl.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Pipelines.Loads;

/// <summary>
/// Checks the options one load row states (FDW-1108): its column lists, its versioning, its soft delete and its value sources, and
/// refuses what is incomplete or contradicts itself, with a reason, before anything is saved or run.
/// </summary>
/// <remarks>
/// <para>
/// The rules each load kind carries are flags on its option (<see cref="ILoadOptionShape"/>), so one check validates the options of any kind and
/// never branches on a name. It checks only what the row says about itself: known names, option pairs that are stated together, and the options
/// the kind needs. It never fills a missing option in: a missing one is a refusal. Whether the columns exist in the data sets is for the API,
/// which validates against the data set schemas.
/// </para>
/// <para>
/// Why a static class: it holds no state, and the graph's destination components and the engine call the same code, so they cannot disagree
/// about what a valid load row is. The checks of a whole stage-list pipeline's load (its mode, its steps, its reject steps, its run parameters)
/// went with that pipeline (FDW-1591).
/// </para>
/// </remarks>
public static class PipelineLoadValidator
{

    internal static IGenericResult ValidateColumnLists(
        string owner,
        PipelineLoadStepConfiguration row,
        ILoadOptionShape shape,
        string shapeName,
        ILogger logger)
    {
        var columns = ValidateColumns(owner, row.Columns, forValueSource: false, logger);
        if (columns.IsFailure)
            return columns;

        var keys = 0;
        var compares = 0;
        var writes = 0;
        foreach (var column in row.Columns)
        {
            var role = PipelineLoadColumnRoles.ByName(column.Role);
            keys += role.IsMatchKey ? 1 : 0;
            compares += role.IsComparison ? 1 : 0;
            writes += role.IsWritten ? 1 : 0;
        }

        var key = CheckOption(owner, "key columns", keys > 0, shape.UsesKeyColumns, shape.UsesKeyColumns, shapeName, logger);
        if (key.IsFailure)
            return key;
        var compare = CheckOption(owner, "compare columns", compares > 0, required: false, allowed: shape.UsesCompareColumns, shapeName, logger);
        return compare.IsFailure
            ? compare
            : CheckOption(owner, "write columns", writes > 0, shape.UsesWriteColumns, shape.UsesWriteColumns, shapeName, logger);
    }

    private static IGenericResult CheckOption(
        string owner,
        string option,
        bool present,
        bool required,
        bool allowed,
        string shapeName,
        ILogger logger)
    {
        if (!present)
        {
            return required
                ? GenericResult.Failure(PipelineLoadLog.LoadStepOptionMissing(logger, owner, option))
                : GenericResult.Success();
        }

        return allowed
            ? GenericResult.Success()
            : GenericResult.Failure(PipelineLoadLog.LoadStepOptionNotApplicable(logger, owner, option, shapeName));
    }

    internal static IGenericResult ValidateVersioning(
        string owner,
        PipelineLoadStepConfiguration row,
        ILoadOptionShape shape,
        string shapeName,
        ILogger logger)
    {
        var hasAny = Has(row.EffectiveFromColumn)
            || Has(row.EffectiveToColumn)
            || Has(row.CurrentFlagColumn)
            || Has(row.InsertOnlyRuleColumn)
            || Has(row.InsertOnlyRuleValue);
        if (!hasAny)
            return GenericResult.Success();
        if (!shape.AllowsVersioning)
            return GenericResult.Failure(PipelineLoadLog.LoadStepOptionNotApplicable(logger, owner, "versioning", shapeName));

        var ends = ValidatePair(
            owner, "versioning", nameof(row.EffectiveFromColumn), row.EffectiveFromColumn, nameof(row.EffectiveToColumn), row.EffectiveToColumn, logger);
        if (ends.IsFailure)
            return ends;
        var rule = ValidatePair(
            owner, "an insert-only rule", nameof(row.InsertOnlyRuleColumn), row.InsertOnlyRuleColumn, nameof(row.InsertOnlyRuleValue), row.InsertOnlyRuleValue, logger);
        if (rule.IsFailure)
            return rule;
        return Has(row.EffectiveFromColumn)
            ? GenericResult.Success()
            : GenericResult.Failure(PipelineLoadLog.LoadOptionPairIncomplete(
                logger, owner, "a current-flag column or an insert-only rule", nameof(row.EffectiveFromColumn)));
    }

    private static IGenericResult ValidatePair(
        string owner,
        string option,
        string firstName,
        string? first,
        string secondName,
        string? second,
        ILogger logger)
    {
        var hasFirst = Has(first);
        if (hasFirst == Has(second))
            return GenericResult.Success();
        return GenericResult.Failure(PipelineLoadLog.LoadOptionPairIncomplete(logger, owner, option, hasFirst ? secondName : firstName));
    }

    // Whether the row states any soft-delete option: the mode, its flag, time and marker columns, or a stage (FDW-4956). One stated without its mode is refused.
    private static bool StatesAnySoftDeleteOption(PipelineLoadStepConfiguration row) =>
        Has(row.SoftDeleteMode)
        || Has(row.DeletedFlagColumn)
        || row.DeletedFlagValue is not null
        || Has(row.DeletedAtColumn)
        || Has(row.SoftDeleteMarkerColumn)
        || Has(row.SoftDeleteMarkerValue)
        || Has(row.StageContainer)
        || Has(row.StageKeyColumn)
        || Has(row.SweepRejectedRowsRule);

    internal static IGenericResult ValidateSoftDelete(
        string owner,
        PipelineLoadStepConfiguration row,
        ILoadOptionShape shape,
        string shapeName,
        ILogger logger)
    {
        var hasMode = Has(row.SoftDeleteMode);
        if (!StatesAnySoftDeleteOption(row))
            return GenericResult.Success();
        if (!shape.AllowsSoftDelete)
            return GenericResult.Failure(PipelineLoadLog.LoadStepOptionNotApplicable(logger, owner, "a soft delete", shapeName));
        if (!hasMode)
            return GenericResult.Failure(PipelineLoadLog.SoftDeleteOptionsInconsistent(logger, owner, "a soft delete names its mode"));

        var mode = SoftDeleteModes.ByName(row.SoftDeleteMode!);
        if (ReferenceEquals(mode, SoftDeleteModes.NotFound))
            return GenericResult.Failure(PipelineLoadLog.SoftDeleteModeUnknown(logger, owner, row.SoftDeleteMode!));
        if (!Has(row.DeletedFlagColumn))
            return GenericResult.Failure(PipelineLoadLog.SoftDeleteOptionsInconsistent(logger, owner, "a soft delete names its flag column"));
        if (row.DeletedFlagValue is null)
            return GenericResult.Failure(PipelineLoadLog.SoftDeleteOptionsInconsistent(logger, owner, "a soft delete states the value it sets its flag column to"));

        var marker = ValidatePair(
            owner, "a soft delete marker", nameof(row.SoftDeleteMarkerColumn), row.SoftDeleteMarkerColumn, nameof(row.SoftDeleteMarkerValue), row.SoftDeleteMarkerValue, logger);
        if (marker.IsFailure)
            return marker;
        if (ReferenceEquals(mode, SoftDeleteModes.AbsentFromStage))
            return GenericResult.Failure(PipelineLoadLog.SoftDeleteOptionsInconsistent(
                logger, owner, "AbsentFromStage is the sweep an AbsentFromSource load runs; a load states AbsentFromSource"));
        return ValidateMarkerUse(owner, row, mode, logger);
    }

    /// <summary>
    /// Checks the unmatched keys rule of a load (FDW-4988). A load whose kind writes existing rows by key (an update, a delete by key) states what a key that
    /// matched no row does, Fail or Warn; any other load, and an absent-from-source delete, which matches nothing by key, takes none.
    /// </summary>
    /// <param name="owner">What the load is, for messages.</param>
    /// <param name="row">The load row.</param>
    /// <param name="kind">The load kind.</param>
    /// <param name="logger">The logger the refusal is written to.</param>
    /// <returns>Success, or the refusal that names what is wrong.</returns>
    internal static IGenericResult ValidateUnmatchedKeys(string owner, PipelineLoadStepConfiguration row, ILoadStepKind kind, ILogger logger)
    {
        if (!MatchesByKey(row, kind))
        {
            return Has(row.UnmatchedKeysRule)
                ? GenericResult.Failure(PipelineLoadLog.LoadStepOptionNotApplicable(logger, owner, "an unmatched keys rule", kind.Name))
                : GenericResult.Success();
        }

        if (!Has(row.UnmatchedKeysRule))
            return GenericResult.Failure(PipelineLoadLog.UnmatchedKeysRuleMissing(logger, owner, kind.Name));
        return ReferenceEquals(UnmatchedKeysRules.ByName(row.UnmatchedKeysRule!), UnmatchedKeysRules.NotFound)
            ? GenericResult.Failure(PipelineLoadLog.UnmatchedKeysRuleUnknown(logger, owner, row.UnmatchedKeysRule!))
            : GenericResult.Success();
    }

    /// <summary>Whether a load finds existing rows by their keys, so a key can match no row (FDW-4988): its kind does, and it is not an absent-from-source delete.</summary>
    /// <param name="row">The load row.</param>
    /// <param name="kind">The load kind.</param>
    /// <returns><see langword="true"/> when the load can miss a key.</returns>
    internal static bool MatchesByKey(PipelineLoadStepConfiguration row, ILoadStepKind kind) =>
        kind.WritesExistingRowsByKey
        && !(Has(row.SoftDeleteMode) && ReferenceEquals(SoftDeleteModes.ByName(row.SoftDeleteMode!), SoftDeleteModes.AbsentFromSource));

    /// <summary>
    /// Checks the stage of a load (FDW-4956). A graph destination that writes a database with an AbsentFromSource soft delete stages each batch's keys in a
    /// container it names and sweeps once at the end, so it holds no rows: its stage container, stage key column and rejected-rows rule are each required.
    /// Every other load (another mode, a file destination, a load step given its rows whole) stages nothing and takes none of them.
    /// </summary>
    /// <param name="owner">What the load is, for messages.</param>
    /// <param name="row">The load row.</param>
    /// <param name="stagesKeys">Whether the load stages its keys.</param>
    /// <param name="logger">The logger the refusal is written to.</param>
    /// <returns>Success, or the refusal that names what is wrong.</returns>
    internal static IGenericResult ValidateStage(string owner, PipelineLoadStepConfiguration row, bool stagesKeys, ILogger logger)
    {
        if (!stagesKeys)
        {
            return Has(row.StageContainer) || Has(row.StageKeyColumn) || Has(row.SweepRejectedRowsRule)
                ? GenericResult.Failure(PipelineLoadLog.SoftDeleteOptionsInconsistent(
                    logger, owner, "only an absent-from-source soft delete into a database stages its keys, so this load takes no stage container, stage key column or rejected-rows rule"))
                : GenericResult.Success();
        }

        if (!Has(row.StageContainer))
            return GenericResult.Failure(PipelineLoadLog.SoftDeleteOptionsInconsistent(logger, owner, "an absent-from-source soft delete names the container its keys are staged in"));
        if (!Has(row.StageKeyColumn))
            return GenericResult.Failure(PipelineLoadLog.SoftDeleteOptionsInconsistent(logger, owner, "an absent-from-source soft delete names its stage container's stage key column"));
        if (!Has(row.SweepRejectedRowsRule))
            return GenericResult.Failure(PipelineLoadLog.SoftDeleteOptionsInconsistent(logger, owner, "an absent-from-source soft delete states what its sweep does when rows were rejected upstream"));
        return ReferenceEquals(SweepRejectedRowsRules.ByName(row.SweepRejectedRowsRule!), SweepRejectedRowsRules.NotFound)
            ? GenericResult.Failure(PipelineLoadLog.SweepRejectedRowsRuleUnknown(logger, owner, row.SweepRejectedRowsRule!))
            : GenericResult.Success();
    }

    private static IGenericResult ValidateMarkerUse(string owner, PipelineLoadStepConfiguration row, ISoftDeleteMode mode, ILogger logger)
    {
        var hasMarker = Has(row.SoftDeleteMarkerColumn);
        if (mode.RequiresMarker && !hasMarker)
            return GenericResult.Failure(PipelineLoadLog.SoftDeleteOptionsInconsistent(logger, owner, $"mode {mode.Name} needs a marker column and a marker value"));
        if (!mode.RequiresMarker && hasMarker)
            return GenericResult.Failure(PipelineLoadLog.SoftDeleteOptionsInconsistent(logger, owner, $"mode {mode.Name} does not use a marker"));
        return GenericResult.Success();
    }

    private static IGenericResult ValidateColumns(
        string owner,
        IList<PipelineLoadColumnConfiguration> columns,
        bool forValueSource,
        ILogger logger)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var column in columns)
        {
            var role = PipelineLoadColumnRoles.ByName(column.Role);
            if (ReferenceEquals(role, PipelineLoadColumnRoles.NotFound))
                return GenericResult.Failure(PipelineLoadLog.LoadColumnRoleUnknown(logger, owner, column.Role));
            if (role.BelongsToValueSource != forValueSource)
                return GenericResult.Failure(PipelineLoadLog.LoadColumnRoleNotForOwner(
                    logger, owner, column.Role, forValueSource ? "only sequence ordering columns belong to a value source" : "sequence ordering columns belong to a value source"));
            if (string.IsNullOrWhiteSpace(column.ColumnName))
                return GenericResult.Failure(PipelineLoadLog.LoadColumnNameBlank(logger, owner, column.Role));
            if (!seen.Add(string.Concat(column.Role, ":", column.Ordinal.ToString(CultureInfo.InvariantCulture))))
                return GenericResult.Failure(PipelineLoadLog.LoadColumnOrdinalDuplicated(logger, owner, column.Role, column.Ordinal));
        }

        return GenericResult.Success();
    }

    internal static IGenericResult ValidateValueSources(
        string owner,
        PipelineLoadStepConfiguration row,
        bool hasSourceDataSet,
        ILogger logger)
    {
        var columnsSeen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var source in row.ValueSources)
        {
            var result = ValidateValueSource(owner, source, hasSourceDataSet, logger);
            if (result.IsFailure)
                return result;
            if (!columnsSeen.Add(source.ColumnName))
                return GenericResult.Failure(PipelineLoadLog.ValueSourceColumnDuplicated(logger, owner, source.ColumnName));
        }

        return GenericResult.Success();
    }

    private static IGenericResult ValidateValueSource(
        string owner,
        PipelineLoadValueSourceConfiguration source,
        bool hasSourceDataSet,
        ILogger logger)
    {
        if (string.IsNullOrWhiteSpace(source.ColumnName))
            return GenericResult.Failure(PipelineLoadLog.ValueSourceOptionMissing(logger, owner, source.Name, "the target column name"));

        var kind = ValueSourceKinds.ByName(source.SourceKind);
        if (ReferenceEquals(kind, ValueSourceKinds.NotFound))
            return GenericResult.Failure(PipelineLoadLog.ValueSourceKindUnknown(logger, owner, source.ColumnName, source.SourceKind));
        if (kind.MapsSourceColumn && !hasSourceDataSet)
            return GenericResult.Failure(PipelineLoadLog.ValueSourceNeedsSourceDataSet(logger, owner, source.ColumnName, kind.Name));

        var field = CheckValueSourceOption(owner, source, "a source field", Has(source.SourceField), kind.RequiresSourceField, kind.RequiresSourceField, kind, logger);
        if (field.IsFailure)
            return field;
        var sequence = ValidateValueSourceSequence(owner, source, kind, logger);
        return sequence.IsFailure ? sequence : ValidateValueSourceConstant(owner, source, kind, logger);
    }

    private static IGenericResult CheckValueSourceOption(
        string owner,
        PipelineLoadValueSourceConfiguration source,
        string option,
        bool present,
        bool required,
        bool allowed,
        IValueSourceKind kind,
        ILogger logger)
    {
        if (present && !allowed)
            return GenericResult.Failure(PipelineLoadLog.ValueSourceOptionNotApplicable(logger, owner, source.ColumnName, option, kind.Name));
        return !present && required
            ? GenericResult.Failure(PipelineLoadLog.ValueSourceOptionMissing(logger, owner, source.ColumnName, option))
            : GenericResult.Success();
    }

    private static IGenericResult ValidateValueSourceSequence(
        string owner,
        PipelineLoadValueSourceConfiguration source,
        IValueSourceKind kind,
        ILogger logger)
    {
        var name = CheckValueSourceOption(owner, source, "a sequence name", Has(source.SequenceName), kind.RequiresSequenceName, kind.RequiresSequenceName, kind, logger);
        if (name.IsFailure)
            return name;
        var schema = CheckValueSourceOption(owner, source, "a sequence schema", Has(source.SequenceSchema), required: false, allowed: kind.RequiresSequenceName, kind: kind, logger: logger);
        if (schema.IsFailure)
            return schema;
        var database = CheckValueSourceOption(owner, source, "a sequence database", Has(source.SequenceDatabase), required: false, allowed: kind.RequiresSequenceName, kind: kind, logger: logger);
        if (database.IsFailure)
            return database;
        var order = CheckValueSourceOption(owner, source, "sequence ordering columns", source.SequenceOrderColumns.Count > 0, kind.RequiresSequenceOrder, kind.RequiresSequenceOrder, kind, logger);
        return order.IsFailure ? order : ValidateColumns(source.ColumnName, source.SequenceOrderColumns, forValueSource: true, logger);
    }

    private static IGenericResult ValidateValueSourceConstant(
        string owner,
        PipelineLoadValueSourceConfiguration source,
        IValueSourceKind kind,
        ILogger logger)
    {
        var hasValue = source.ConstantValue is not null;
        var hasType = Has(source.ConstantDataType);
        if (!kind.RequiresConstant)
        {
            return hasValue || hasType
                ? GenericResult.Failure(PipelineLoadLog.ValueSourceOptionNotApplicable(logger, owner, source.ColumnName, "a constant", kind.Name))
                : GenericResult.Success();
        }

        if (!hasValue || !hasType)
            return GenericResult.Failure(PipelineLoadLog.ValueSourceOptionMissing(logger, owner, source.ColumnName, "a constant value and the type that reads it"));
        return ReferenceEquals(DataTypes.ByName(source.ConstantDataType!), DataTypes.NotFound)
            ? GenericResult.Failure(PipelineLoadLog.ValueSourceConstantTypeUnknown(logger, owner, source.ColumnName, source.ConstantDataType!))
            : GenericResult.Success();
    }

    private static bool Has(string? value) => !string.IsNullOrWhiteSpace(value);
}
