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
/// Checks the structure of a BatchCopy pipeline's load configuration (FDW-1108) and refuses what is incomplete or
/// contradicts itself, with a reason, before anything is saved or run.
/// </summary>
/// <remarks>
/// <para>
/// A pipeline states a load mode (and, for Upsert, load options) or load steps, never both. The rules each mode and
/// each step kind carries are flags on its option, so one check validates the options of either and never branches
/// on a name. Beyond that it checks only what the configuration says about itself: known names, step orders and
/// names that are not repeated, option pairs that are stated together, and the options each kind needs. It never
/// fills a missing option in: a missing one is a refusal. Whether the columns exist in the data sets is for the API,
/// which validates against the data set schemas.
/// </para>
/// <para>
/// Why a static class: it holds no state, and the API's create, update and validate paths and the engine all call
/// the same code, so they cannot disagree about what a valid load is.
/// </para>
/// </remarks>
public static class PipelineLoadValidator
{
    private const string OwnLoadLabel = "the pipeline's own load";

    /// <summary>
    /// Validates the load configuration of one pipeline.
    /// </summary>
    /// <param name="loadMode">The pipeline's load mode name, or <see langword="null"/> when it states none.</param>
    /// <param name="loadSteps">
    /// The pipeline's load rows, each stating its phase: the row of its own load (step order <see langword="null"/>), its
    /// load steps and its reject steps, or <see langword="null"/> when it states none.
    /// </param>
    /// <param name="runParameters">The pipeline's declared run parameters, or <see langword="null"/> when it declares none.</param>
    /// <param name="pipelineDestinationDataSet">The data set the pipeline's own load writes to, or <see langword="null"/> when its destination is not a data set; a reject step may not write there.</param>
    /// <param name="logger">The logger the refusals are written to.</param>
    /// <returns>Success, or a failure whose message says what is wrong.</returns>
    public static IGenericResult Validate(
        string? loadMode,
        IList<PipelineLoadStepConfiguration>? loadSteps,
        IList<PipelineRunParameterConfiguration>? runParameters,
        string? pipelineDestinationDataSet,
        ILogger logger)
    {
        var phased = SplitByPhase(loadSteps, logger);
        if (phased.IsFailure)
            return phased;

        var load = ValidateLoadPhase(loadMode, phased.Value!.Loading, logger);
        if (load.IsFailure)
            return load;

        var reject = PipelineRejectStepValidator.Validate(phased.Value.Rejecting, phased.Value.Loading, pipelineDestinationDataSet, logger);
        return reject.IsFailure ? reject : ValidateRunParameters(runParameters, logger);
    }

    // Every row states a phase that names one of the load phases; none is assumed. The rows that load the output and the
    // reject steps are then judged each by their own rules.
    private static IGenericResult<PhasedRows> SplitByPhase(IList<PipelineLoadStepConfiguration>? rows, ILogger logger)
    {
        var loading = new List<PipelineLoadStepConfiguration>();
        var rejecting = new List<PipelineLoadStepConfiguration>();
        foreach (var row in rows is null ? [] : rows)
        {
            var owner = row.StepOrder is null ? OwnLoadLabel : string.Concat("load step '", row.Name, "'");
            if (string.IsNullOrWhiteSpace(row.Phase))
                return GenericResult<PhasedRows>.Failure(PipelineLoadLog.LoadPhaseMissing(logger, owner));

            var phase = LoadPhases.ByName(row.Phase);
            if (ReferenceEquals(phase, LoadPhases.NotFound))
                return GenericResult<PhasedRows>.Failure(PipelineLoadLog.LoadPhaseUnknown(logger, owner, row.Phase));
            (phase.ReadsRejectedRecords ? rejecting : loading).Add(row);
        }

        return GenericResult<PhasedRows>.Success(new PhasedRows(loading, rejecting));
    }

    private static IGenericResult ValidateLoadPhase(string? loadMode, List<PipelineLoadStepConfiguration> rows, ILogger logger)
    {
        PipelineLoadStepConfiguration? ownLoad = null;
        var ownLoadCount = 0;
        var steps = new List<(int Order, PipelineLoadStepConfiguration Step)>();
        foreach (var row in rows)
        {
            if (row.StepOrder is { } order)
            {
                steps.Add((order, row));
            }
            else
            {
                ownLoad = row;
                ownLoadCount++;
            }
        }

        if (ownLoadCount > 1)
            return GenericResult.Failure(PipelineLoadLog.OwnLoadRepeated(logger));

        return steps.Count == 0
            ? ValidateImplicitLoad(loadMode, ownLoad, logger)
            : ValidateStepLoad(loadMode, ownLoad, steps, logger);
    }

    private sealed record PhasedRows(List<PipelineLoadStepConfiguration> Loading, List<PipelineLoadStepConfiguration> Rejecting);

    private static IGenericResult ValidateImplicitLoad(string? loadMode, PipelineLoadStepConfiguration? ownLoad, ILogger logger)
    {
        if (string.IsNullOrWhiteSpace(loadMode))
            return GenericResult.Failure(PipelineLoadLog.LoadModeMissing(logger));

        var mode = LoadModes.ByName(loadMode);
        if (ReferenceEquals(mode, LoadModes.NotFound))
            return GenericResult.Failure(PipelineLoadLog.PipelineLoadModeUnknown(logger, loadMode));
        if (ownLoad is null)
        {
            return mode.UsesKeyColumns || mode.UsesWriteColumns
                ? GenericResult.Failure(PipelineLoadLog.LoadModeNeedsOptions(logger, loadMode))
                : GenericResult.Success();
        }

        var fields = ValidateOwnLoadFields(ownLoad, logger);
        if (fields.IsFailure)
            return fields;
        var options = ValidateOptions(OwnLoadLabel, ownLoad, mode, loadMode, logger);
        return options.IsFailure ? options : ValidateValueSources(OwnLoadLabel, ownLoad, hasSourceDataSet: false, forContainer: false, logger);
    }

    private static IGenericResult ValidateOwnLoadFields(PipelineLoadStepConfiguration ownLoad, ILogger logger)
    {
        if (Has(ownLoad.Kind))
            return GenericResult.Failure(PipelineLoadLog.OwnLoadStatesStepOption(logger, "a kind"));
        if (Has(ownLoad.DestinationDataSet))
            return GenericResult.Failure(PipelineLoadLog.OwnLoadStatesStepOption(logger, "a destination data set"));
        if (Has(ownLoad.SourceDataSet))
            return GenericResult.Failure(PipelineLoadLog.OwnLoadStatesStepOption(logger, "a source data set"));
        if (Has(ownLoad.FilterExpression))
            return GenericResult.Failure(PipelineLoadLog.OwnLoadStatesStepOption(logger, "a filter expression"));
        return Has(ownLoad.DestinationDataStoreName) || Has(ownLoad.DestinationPathName) || Has(ownLoad.DestinationContainerName)
            ? GenericResult.Failure(PipelineLoadLog.OwnLoadStatesStepOption(logger, "a destination container"))
            : GenericResult.Success();
    }

    private static IGenericResult ValidateStepLoad(
        string? loadMode,
        PipelineLoadStepConfiguration? ownLoad,
        List<(int Order, PipelineLoadStepConfiguration Step)> steps,
        ILogger logger)
    {
        if (!string.IsNullOrWhiteSpace(loadMode))
            return GenericResult.Failure(PipelineLoadLog.LoadModeWithSteps(logger, loadMode));
        if (ownLoad is not null)
            return GenericResult.Failure(PipelineLoadLog.OwnLoadWithSteps(logger));

        var seenOrders = new HashSet<int>();
        var seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (order, step) in steps)
        {
            var identity = ValidateStepIdentity(order, step, steps.Count, seenOrders, seenNames, logger);
            if (identity.IsFailure)
                return identity;
            var result = ValidateStep(step, string.Concat("load step '", step.Name, "'"), logger);
            if (result.IsFailure)
                return result;
        }

        return GenericResult.Success();
    }

    internal static IGenericResult ValidateStepIdentity(
        int order,
        PipelineLoadStepConfiguration step,
        int stepCount,
        HashSet<int> seenOrders,
        HashSet<string> seenNames,
        ILogger logger)
    {
        if (string.IsNullOrWhiteSpace(step.Name))
            return GenericResult.Failure(PipelineLoadLog.LoadStepNameBlank(logger, order));
        if (order < 1 || order > stepCount)
            return GenericResult.Failure(PipelineLoadLog.LoadStepOrderInvalid(logger, step.Name, order, stepCount));
        if (!seenOrders.Add(order))
            return GenericResult.Failure(PipelineLoadLog.LoadStepOrderDuplicated(logger, order));
        return seenNames.Add(step.Name)
            ? GenericResult.Success()
            : GenericResult.Failure(PipelineLoadLog.LoadStepNameDuplicated(logger, step.Name));
    }

    internal static IGenericResult ValidateStep(PipelineLoadStepConfiguration step, string owner, ILogger logger)
    {
        if (string.IsNullOrWhiteSpace(step.Kind))
            return GenericResult.Failure(PipelineLoadLog.LoadStepKindMissing(logger, owner));

        var kind = LoadStepKinds.ByName(step.Kind);
        if (ReferenceEquals(kind, LoadStepKinds.NotFound))
            return GenericResult.Failure(PipelineLoadLog.LoadStepKindUnknown(logger, owner, step.Kind));
        if (!Has(step.DestinationDataSet))
            return GenericResult.Failure(PipelineLoadLog.LoadStepDestinationMissing(logger, owner));

        var fields = ValidateKindFields(owner, step, kind, logger);
        if (fields.IsFailure)
            return fields;
        var options = ValidateOptions(owner, step, kind, kind.Name, logger);
        return options.IsFailure ? options : ValidateValueSources(owner, step, kind.UsesSourceDataSet, kind.UsesDestinationContainer, logger);
    }

    private static IGenericResult ValidateKindFields(string owner, PipelineLoadStepConfiguration step, ILoadStepKind kind, ILogger logger)
    {
        var source = CheckOption(owner, "a source data set", Has(step.SourceDataSet), kind.UsesSourceDataSet, kind.UsesSourceDataSet, kind.Name, logger);
        if (source.IsFailure)
            return source;
        var filter = CheckOption(owner, "a filter expression", Has(step.FilterExpression), required: false, allowed: kind.UsesSourceDataSet, kind.Name, logger);
        if (filter.IsFailure)
            return filter;
        var store = CheckOption(owner, "a destination data store", Has(step.DestinationDataStoreName), kind.UsesDestinationContainer, kind.UsesDestinationContainer, kind.Name, logger);
        if (store.IsFailure)
            return store;
        var path = CheckOption(owner, "a destination container path", Has(step.DestinationPathName), kind.UsesDestinationContainer, kind.UsesDestinationContainer, kind.Name, logger);
        return path.IsFailure
            ? path
            : CheckOption(owner, "a destination container name", Has(step.DestinationContainerName), kind.UsesDestinationContainer, kind.UsesDestinationContainer, kind.Name, logger);
    }

    private static IGenericResult ValidateOptions(
        string owner,
        PipelineLoadStepConfiguration row,
        ILoadOptionShape shape,
        string shapeName,
        ILogger logger)
    {
        var columns = ValidateColumnLists(owner, row, shape, shapeName, logger);
        if (columns.IsFailure)
            return columns;
        var versioning = ValidateVersioning(owner, row, shape, shapeName, logger);
        return versioning.IsFailure ? versioning : ValidateSoftDelete(owner, row, shape, shapeName, logger);
    }

    private static IGenericResult ValidateColumnLists(
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

    private static IGenericResult ValidateVersioning(
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

    private static IGenericResult ValidateSoftDelete(
        string owner,
        PipelineLoadStepConfiguration row,
        ILoadOptionShape shape,
        string shapeName,
        ILogger logger)
    {
        var hasMode = Has(row.SoftDeleteMode);
        var hasAny = hasMode
            || Has(row.DeletedFlagColumn)
            || Has(row.DeletedAtColumn)
            || Has(row.SoftDeleteMarkerColumn)
            || Has(row.SoftDeleteMarkerValue);
        if (!hasAny)
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

        var marker = ValidatePair(
            owner, "a soft delete marker", nameof(row.SoftDeleteMarkerColumn), row.SoftDeleteMarkerColumn, nameof(row.SoftDeleteMarkerValue), row.SoftDeleteMarkerValue, logger);
        return marker.IsFailure ? marker : ValidateMarkerUse(owner, row, mode, logger);
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

    private static IGenericResult ValidateValueSources(
        string owner,
        PipelineLoadStepConfiguration row,
        bool hasSourceDataSet,
        bool forContainer,
        ILogger logger)
    {
        var columnsSeen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var source in row.ValueSources)
        {
            var result = ValidateValueSource(owner, source, hasSourceDataSet, forContainer, logger);
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
        bool forContainer,
        ILogger logger)
    {
        if (string.IsNullOrWhiteSpace(source.ColumnName))
            return GenericResult.Failure(PipelineLoadLog.ValueSourceOptionMissing(logger, owner, source.Name, "the target column name"));

        var kind = ValueSourceKinds.ByName(source.SourceKind);
        if (ReferenceEquals(kind, ValueSourceKinds.NotFound))
            return GenericResult.Failure(PipelineLoadLog.ValueSourceKindUnknown(logger, owner, source.ColumnName, source.SourceKind));
        if (kind.MapsSourceColumn && !hasSourceDataSet)
            return GenericResult.Failure(PipelineLoadLog.ValueSourceNeedsSourceDataSet(logger, owner, source.ColumnName, kind.Name));
        if (forContainer && !kind.AllowedAsContainerParameter)
            return GenericResult.Failure(PipelineLoadLog.ValueSourceNotForContainer(logger, owner, source.ColumnName, kind.Name));

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

    private static IGenericResult ValidateRunParameters(IList<PipelineRunParameterConfiguration>? runParameters, ILogger logger)
    {
        if (runParameters is null)
            return GenericResult.Success();

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var parameter in runParameters)
        {
            if (string.IsNullOrWhiteSpace(parameter.Name))
                return GenericResult.Failure(PipelineLoadLog.RunParameterNameBlank(logger));
            if (!seen.Add(parameter.Name))
                return GenericResult.Failure(PipelineLoadLog.RunParameterNameDuplicated(logger, parameter.Name));
            if (!IsRunParameterType(parameter.ParameterType))
                return GenericResult.Failure(PipelineLoadLog.RunParameterTypeUnknown(logger, parameter.Name, parameter.ParameterType));
        }

        return GenericResult.Success();
    }

    // A run parameter's type is a data type whose values have a text form that fits in an expression.
    private static bool IsRunParameterType(string name)
    {
        var type = DataTypes.ByName(name);
        return !ReferenceEquals(type, DataTypes.NotFound) && type.RendersInExpressions;
    }

    private static bool Has(string? value) => !string.IsNullOrWhiteSpace(value);
}
