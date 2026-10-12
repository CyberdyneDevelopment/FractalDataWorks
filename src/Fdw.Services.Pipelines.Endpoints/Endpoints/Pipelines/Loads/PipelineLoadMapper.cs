using System.Collections.Generic;
using System.Linq;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.OptionTypes;
using Fdw.Services.Etl.Pipelines.Loads;
using Fdw.Services.Pipelines.Clients.Abstractions;
using Fdw.Services.Pipelines.Endpoints.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// Maps the load definition a pipeline body carries onto the engine's configuration rows (FDW-1110, contract v1 to
/// v4). It maps and refuses what cannot be mapped; whether the definition is valid is
/// <see cref="PipelineLoadValidator"/>'s call, the same code the engine uses.
/// </summary>
/// <remarks>
/// Why nothing is defaulted: an absent load mode stays absent (the validator refuses it when there are no steps), an
/// absent option stays null, and a step's order must equal its position -- the body is never corrected on the caller's
/// behalf.
/// </remarks>
public static class PipelineLoadMapper
{
    private const string KeyRole = "Key";
    private const string CompareRole = "Compare";
    private const string WriteRole = "Write";
    private const string SequenceOrderRole = "SequenceOrder";

    /// <summary>Maps the load fields of a pipeline body.</summary>
    /// <param name="fields">The body's load fields.</param>
    /// <param name="logger">The logger a refusal is written to.</param>
    /// <returns>The mapped load, or a failure naming what the body states that cannot be mapped.</returns>
    public static IGenericResult<MappedPipelineLoad> Map(IPipelineLoadFields fields, ILogger logger)
    {
        var mode = string.IsNullOrWhiteSpace(fields.LoadMode) ? null : fields.LoadMode;
        var refused = RefuseOptionsOfOptionlessMode(mode, fields.LoadOptions, logger);
        if (refused is not null)
            return GenericResult<MappedPipelineLoad>.Failure(refused);

        var rows = MapRows(fields, logger);
        if (rows.IsFailure)
            return rows.ToNewResult<MappedPipelineLoad>();

        var parameters = Items(fields.Parameters).Select(MapParameter).ToList();
        return GenericResult<MappedPipelineLoad>.Success(
            new MappedPipelineLoad(mode, rows.Value!.Count > 0 ? rows.Value : null, parameters.Count > 0 ? parameters : null));
    }

    /// <summary>Maps one load step to its configuration row at the given order.</summary>
    /// <param name="step">The step as the body states it.</param>
    /// <param name="order">The step order the row takes.</param>
    /// <param name="phase">The phase the row runs in: Load for a load step, Reject for a reject step.</param>
    /// <returns>The step's configuration row.</returns>
    public static PipelineLoadStepConfiguration MapStep(PipelineLoadStep step, int order, ILoadPhase phase)
    {
        var options = step.Options is null ? new PipelineLoadStepOptions() : step.Options;
        var row = new PipelineLoadStepConfiguration
        {
            Name = step.Name,
            Phase = phase.Name,
            StepOrder = order,
            Kind = step.Kind,
            DestinationDataSet = step.DestinationDataSet,
            SourceDataSet = options.SourceDataSet,
            FilterExpression = options.FilterExpression,
            IsEnabled = step.IsEnabled,
        };
        if (options.DestinationContainer is { } container)
        {
            row.DestinationDataStoreName = container.DataStoreName;
            row.DestinationPathName = container.PathName;
            row.DestinationContainerName = container.ContainerName;
        }

        ApplyOptions(row, options);
        row.ValueSources = [.. Items(step.ValueSources).Select(MapColumnSource), .. Items(options.Parameters).Select(MapExecuteParameter)];
        return row;
    }

    // Why a helper and not a default: a list the body leaves out states no entries, and the validator refuses a
    // definition that needed some. Nothing is filled in.
    private static IEnumerable<T> Items<T>(IEnumerable<T>? list) => list is null ? [] : list;

    private static IGenericResult<List<PipelineLoadStepConfiguration>> MapRows(IPipelineLoadFields fields, ILogger logger)
    {
        var rows = new List<PipelineLoadStepConfiguration>();
        if (fields.LoadOptions is not null || Items(fields.ValueSources).Any())
            rows.Add(MapOwnLoad(fields.LoadOptions, fields.ValueSources));

        var loading = MapSteps(fields.LoadSteps, LoadPhases.Load, rows, logger);
        return loading.IsFailure
            ? loading.ToNewResult<List<PipelineLoadStepConfiguration>>()
            : MapSteps(fields.RejectSteps, LoadPhases.Reject, rows, logger);
    }

    // The steps of one phase are ordered from 1 within the phase; a step's order must equal its position.
    private static IGenericResult<List<PipelineLoadStepConfiguration>> MapSteps(
        IList<PipelineLoadStep>? stated,
        ILoadPhase phase,
        List<PipelineLoadStepConfiguration> rows,
        ILogger logger)
    {
        var steps = Items(stated).ToList();
        for (var position = 0; position < steps.Count; position++)
        {
            if (steps[position].Order != position + 1)
            {
                return GenericResult<List<PipelineLoadStepConfiguration>>.Failure(
                    PipelineEndpointLog.LoadStepOrderNotPosition(logger, steps[position].Name, steps[position].Order, position + 1));
            }

            rows.Add(MapStep(steps[position], steps[position].Order, phase));
        }

        return GenericResult<List<PipelineLoadStepConfiguration>>.Success(rows);
    }

    private static PipelineLoadStepConfiguration MapOwnLoad(PipelineLoadOptions? options, IList<PipelineLoadColumnSource>? valueSources)
    {
        var row = new PipelineLoadStepConfiguration
        {
            Name = PipelineLoadStepConfiguration.PipelineLoadName,
            Phase = LoadPhases.Load.Name,
            StepOrder = null,
            IsEnabled = true,
            ValueSources = [.. Items(valueSources).Select(MapColumnSource)],
        };
        if (options is not null)
            ApplyOptions(row, options);
        return row;
    }

    // A load mode that takes no columns (Append, Replace) refuses load options rather than ignoring them (contract v2 item 6).
    private static IGenericMessage? RefuseOptionsOfOptionlessMode(string? mode, PipelineLoadOptions? options, ILogger logger)
    {
        if (options is null || mode is null)
            return null;
        var loadMode = LoadModes.ByName(mode);
        return ReferenceEquals(loadMode, LoadModes.NotFound) || loadMode.UsesKeyColumns || loadMode.UsesWriteColumns
            ? null
            : PipelineEndpointLog.LoadOptionsNotForMode(logger, mode);
    }

    private static void ApplyOptions(PipelineLoadStepConfiguration row, PipelineLoadOptions options)
    {
        row.Columns =
        [
            .. Columns(KeyRole, Items(options.KeyColumns)),
            .. Columns(CompareRole, Items(options.CompareColumns)),
            .. Columns(WriteRole, Items(options.WriteColumns)),
        ];
        if (options.Versioning is { } versioning)
        {
            row.EffectiveFromColumn = versioning.EffectiveFromColumn;
            row.EffectiveToColumn = versioning.EffectiveToColumn;
            row.CurrentFlagColumn = versioning.CurrentFlagColumn;
            row.InsertOnlyRuleColumn = versioning.InsertOnlyRule?.Column;
            row.InsertOnlyRuleValue = versioning.InsertOnlyRule?.Value;
        }

        if (options.SoftDelete is { } softDelete)
            ApplySoftDelete(row, softDelete);
    }

    private static void ApplySoftDelete(PipelineLoadStepConfiguration row, PipelineLoadSoftDelete softDelete)
    {
        row.SoftDeleteMode = softDelete.Mode;
        row.DeletedFlagColumn = softDelete.DeletedFlagColumn;
        row.DeletedAtColumn = softDelete.DeletedAtColumn;
        row.SoftDeleteMarkerColumn = softDelete.Marker?.Column;
        row.SoftDeleteMarkerValue = softDelete.Marker?.Value;
    }

    private static IEnumerable<PipelineLoadColumnConfiguration> Columns(string role, IEnumerable<string> names) =>
        names.Select((name, ordinal) => new PipelineLoadColumnConfiguration { Name = name, Role = role, ColumnName = name, Ordinal = ordinal });

    private static PipelineLoadValueSourceConfiguration MapColumnSource(PipelineLoadColumnSource entry) =>
        MapSource(entry.Column, entry.Source);

    private static PipelineLoadValueSourceConfiguration MapExecuteParameter(PipelineLoadExecuteParameter parameter) =>
        MapSource(parameter.Name, parameter.Source);

    // A source the body leaves out has no kind, which the validator refuses with the column named.
    private static PipelineLoadValueSourceConfiguration MapSource(string column, PipelineLoadValueSource? stated)
    {
        var source = stated is null ? new PipelineLoadValueSource() : stated;
        return new()
        {
            Name = column,
            ColumnName = column,
            SourceKind = source.Kind,
            SourceField = source.SourceField,
            SequenceName = source.SequenceName,
            SequenceSchema = source.Schema,
            SequenceDatabase = source.Database,
            ConstantValue = source.Value,
            ConstantDataType = source.ValueType,
            SequenceOrderColumns = [.. Columns(SequenceOrderRole, Items(source.OrderBy))],
        };
    }

    private static PipelineRunParameterConfiguration MapParameter(PipelineParameterDeclaration declaration) => new()
    {
        Name = declaration.Name,
        ParameterType = declaration.Type,
        IsRequired = declaration.Required,
        Description = declaration.Description,
    };
}
