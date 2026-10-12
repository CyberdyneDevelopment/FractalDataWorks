using System.Collections.Generic;
using System.Linq;
using Fdw.Services.Etl.Abstractions.OptionTypes;
using Fdw.Services.Etl.Pipelines.Loads;
using Fdw.Services.Pipelines.Clients.Abstractions;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// Reads a pipeline's stored load definition back into the shapes the body carries (FDW-1110, contract item 5): every
/// field exactly as saved. The inverse of <see cref="PipelineLoadMapper"/>.
/// </summary>
/// <remarks>
/// Why the null-forgiving operator on stored text: nothing is filled in for a value the row lacks. The validator that
/// gates every save refuses a step without a kind or destination data set, versioning without both of its effective
/// columns, and a marker or insert-only rule without its value, so a stored row that reaches this reader has them.
/// </remarks>
public static class PipelineLoadReader
{
    private const string KeyRole = "Key";
    private const string CompareRole = "Compare";
    private const string WriteRole = "Write";

    /// <summary>The load mode name stored on the pipeline, or <see langword="null"/> when it states load steps.</summary>
    /// <param name="load">The pipeline's load configuration.</param>
    /// <returns>The load mode name.</returns>
    public static string? LoadMode(IPipelineLoadConfiguration load) => load.LoadMode;

    /// <summary>Reads the options of the pipeline's own load (the row with no step order).</summary>
    /// <param name="load">The pipeline's load configuration.</param>
    /// <returns>The options, or <see langword="null"/> when the pipeline has no own-load row.</returns>
    public static PipelineLoadOptions? Options(IPipelineLoadConfiguration load) =>
        OwnLoad(load) is { } row ? ReadOptions(row, new PipelineLoadOptions()) : null;

    /// <summary>Reads the value sources of the pipeline's own load.</summary>
    /// <param name="load">The pipeline's load configuration.</param>
    /// <returns>The value sources, or <see langword="null"/> when the pipeline has no own-load row.</returns>
    public static IList<PipelineLoadColumnSource>? ValueSources(IPipelineLoadConfiguration load) =>
        OwnLoad(load) is { } row ? [.. row.ValueSources.Select(ReadColumnSource)] : null;

    /// <summary>Reads the load steps in step order.</summary>
    /// <param name="load">The pipeline's load configuration.</param>
    /// <returns>The steps, or <see langword="null"/> when the pipeline has none.</returns>
    public static IList<PipelineLoadStep>? Steps(IPipelineLoadConfiguration load) => ReadSteps(LoadPhaseRows.Loading(load.LoadSteps));

    /// <summary>Reads the reject steps in step order.</summary>
    /// <param name="load">The pipeline's load configuration.</param>
    /// <returns>The reject steps, or <see langword="null"/> when the pipeline has none.</returns>
    public static IList<PipelineLoadStep>? RejectSteps(IPipelineLoadConfiguration load) => ReadSteps(LoadPhaseRows.Rejecting(load.LoadSteps));

    /// <summary>Reads the declared run parameters.</summary>
    /// <param name="load">The pipeline's load configuration.</param>
    /// <returns>The declarations, or <see langword="null"/> when the pipeline declares none.</returns>
    public static IList<PipelineParameterDeclaration>? Parameters(IPipelineLoadConfiguration load) =>
        load.RunParameters is { Count: > 0 } rows ? [.. rows.Select(ReadParameter)] : null;

    private static List<PipelineLoadStep>? ReadSteps(IEnumerable<PipelineLoadStepConfiguration> rows)
    {
        var steps = rows.Where(r => r.StepOrder is not null).OrderBy(r => r.StepOrder).Select(ReadStep).ToList();
        return steps.Count > 0 ? steps : null;
    }

    private static PipelineLoadStepConfiguration? OwnLoad(IPipelineLoadConfiguration load) =>
        load.LoadSteps?.FirstOrDefault(r => r.StepOrder is null);

    private static PipelineLoadStep ReadStep(PipelineLoadStepConfiguration row)
    {
        var options = ReadOptions(row, new PipelineLoadStepOptions());
        options.SourceDataSet = row.SourceDataSet;
        options.FilterExpression = row.FilterExpression;
        var carriesParameters = LoadStepKinds.ByName(row.Kind!).UsesDestinationContainer;
        if (carriesParameters)
        {
            options.DestinationContainer = ReadContainer(row);
            options.Parameters = [.. row.ValueSources.Select(ReadExecuteParameter)];
        }

        return new PipelineLoadStep
        {
            Order = row.StepOrder!.Value,
            Name = row.Name,
            IsEnabled = row.IsEnabled,
            Kind = row.Kind!,
            DestinationDataSet = row.DestinationDataSet!,
            Options = options,
            ValueSources = carriesParameters ? [] : [.. row.ValueSources.Select(ReadColumnSource)],
        };
    }

    private static PipelineLoadDestinationContainer ReadContainer(PipelineLoadStepConfiguration row) => new()
    {
        DataStoreName = row.DestinationDataStoreName!,
        PathName = row.DestinationPathName!,
        ContainerName = row.DestinationContainerName!,
    };

    private static T ReadOptions<T>(PipelineLoadStepConfiguration row, T options)
        where T : PipelineLoadOptions
    {
        options.KeyColumns = ColumnNames(row, KeyRole);
        options.CompareColumns = ColumnNames(row, CompareRole);
        options.WriteColumns = ColumnNames(row, WriteRole);
        options.Versioning = ReadVersioning(row);
        options.SoftDelete = ReadSoftDelete(row);
        return options;
    }

    private static List<string> ColumnNames(PipelineLoadStepConfiguration row, string role) =>
        [.. row.Columns.Where(c => string.Equals(c.Role, role, System.StringComparison.OrdinalIgnoreCase)).OrderBy(c => c.Ordinal).Select(c => c.ColumnName)];

    // Versioning is stated by its two effective columns, which the validator requires together.
    private static PipelineLoadVersioning? ReadVersioning(PipelineLoadStepConfiguration row) =>
        string.IsNullOrWhiteSpace(row.EffectiveFromColumn) && string.IsNullOrWhiteSpace(row.EffectiveToColumn)
            ? null
            : new PipelineLoadVersioning
            {
                EffectiveFromColumn = row.EffectiveFromColumn!,
                EffectiveToColumn = row.EffectiveToColumn!,
                CurrentFlagColumn = row.CurrentFlagColumn,
                InsertOnlyRule = string.IsNullOrWhiteSpace(row.InsertOnlyRuleColumn)
                    ? null
                    : new PipelineLoadInsertOnlyRule { Column = row.InsertOnlyRuleColumn, Value = row.InsertOnlyRuleValue! },
            };

    private static PipelineLoadSoftDelete? ReadSoftDelete(PipelineLoadStepConfiguration row) =>
        string.IsNullOrWhiteSpace(row.SoftDeleteMode)
            ? null
            : new PipelineLoadSoftDelete
            {
                Mode = row.SoftDeleteMode,
                DeletedFlagColumn = row.DeletedFlagColumn!,
                DeletedAtColumn = row.DeletedAtColumn,
                Marker = string.IsNullOrWhiteSpace(row.SoftDeleteMarkerColumn)
                    ? null
                    : new PipelineLoadMarker { Column = row.SoftDeleteMarkerColumn, Value = row.SoftDeleteMarkerValue! },
            };

    private static PipelineLoadColumnSource ReadColumnSource(PipelineLoadValueSourceConfiguration source) =>
        new() { Column = source.ColumnName, Source = ReadSource(source) };

    private static PipelineLoadExecuteParameter ReadExecuteParameter(PipelineLoadValueSourceConfiguration source) =>
        new() { Name = source.ColumnName, Source = ReadSource(source) };

    private static PipelineLoadValueSource ReadSource(PipelineLoadValueSourceConfiguration source) => new()
    {
        Kind = source.SourceKind,
        SourceField = source.SourceField,
        ValueType = source.ConstantDataType,
        Value = source.ConstantValue,
        SequenceName = source.SequenceName,
        OrderBy = [.. source.SequenceOrderColumns.OrderBy(c => c.Ordinal).Select(c => c.ColumnName)],
        Schema = source.SequenceSchema,
        Database = source.SequenceDatabase,
    };

    private static PipelineParameterDeclaration ReadParameter(PipelineRunParameterConfiguration row) => new()
    {
        Name = row.Name,
        Type = row.ParameterType,
        Required = row.IsRequired,
        Description = row.Description,
    };
}
