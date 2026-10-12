using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.OptionTypes;
using Fdw.Services.Etl.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Pipelines.Loads;

/// <summary>
/// What a run does to load its output, worked out once from the pipeline's configuration (FDW-1109): the load mode or the
/// ordered load steps, checked by the shared <see cref="PipelineLoadValidator"/> first.
/// </summary>
/// <remarks>
/// <para>
/// The pipeline's own load (a load mode with its options) is one unit that runs page by page as the output arrives, as the
/// engine always loaded. Load steps run through a unit of work and need the whole output at once, so a pipeline with steps,
/// or an own load that flags the keys absent from the source, is run on the complete set.
/// </para>
/// <para>
/// <c>TruncateBeforeLoad</c> is the old way to state Replace. It is accepted only beside load mode Replace, where it says
/// the same thing; beside any other load definition it contradicts it and the run is refused.
/// </para>
/// </remarks>
public sealed class PipelineLoadPlan
{
    private PipelineLoadPlan(
        LoadUnit? ownLoad,
        IReadOnlyList<LoadUnit> steps,
        bool truncatesFirst,
        bool hasSteps,
        bool keyed,
        bool flagsAbsentKeys,
        List<LoadUnit> rejectSteps)
    {
        OwnLoad = ownLoad;
        Steps = steps;
        TruncatesFirst = truncatesFirst;
        HasSteps = hasSteps;
        Keyed = keyed;
        FlagsAbsentKeys = flagsAbsentKeys;
        RejectSteps = rejectSteps;
    }

    /// <summary>Gets the pipeline's own load, or <see langword="null"/> when the pipeline states load steps instead.</summary>
    public LoadUnit? OwnLoad { get; }

    /// <summary>Gets the enabled load steps, in order.</summary>
    public IReadOnlyList<LoadUnit> Steps { get; }

    /// <summary>Gets whether the destination is truncated once before the first row is written (load mode Replace).</summary>
    public bool TruncatesFirst { get; }

    /// <summary>Gets whether the pipeline states load steps.</summary>
    public bool HasSteps { get; }

    /// <summary>Gets whether the pipeline's own load matches rows on key columns (Upsert), which only a data store can do.</summary>
    public bool Keyed { get; }

    /// <summary>Gets whether the pipeline's own load flags the keys absent from the source, so it needs the whole output.</summary>
    public bool FlagsAbsentKeys { get; }

    /// <summary>Gets the enabled reject steps, in order: they write the records the pipeline's stages rejected, after the load (FDW-1142).</summary>
    public IReadOnlyList<LoadUnit> RejectSteps { get; }

    /// <summary>Gets whether the output must be gathered whole before it is loaded.</summary>
    public bool NeedsCompleteSet => HasSteps || FlagsAbsentKeys;

    /// <summary>Works out the plan of a pipeline.</summary>
    /// <param name="configuration">The pipeline.</param>
    /// <param name="logger">The logger the refusals are written to.</param>
    /// <returns>The plan, or a failure saying what the load definition gets wrong.</returns>
    public static IGenericResult<PipelineLoadPlan> Create(BatchCopyPipelineConfiguration configuration, ILogger logger)
    {
        var valid = PipelineLoadValidator.Validate(
            configuration.LoadMode, configuration.LoadSteps, configuration.RunParameters, configuration.DestinationDataSet, logger);
        if (valid.IsFailure)
            return valid.ToNewResult<PipelineLoadPlan>();

        var rows = LoadPhaseRows.Loading(configuration.LoadSteps);
        return rows.Any(row => row.StepOrder is not null)
            ? ForSteps(configuration, rows, logger)
            : ForOwnLoad(configuration, rows.FirstOrDefault(), logger);
    }

    // The enabled reject steps, in order: written after the load, whichever way the load itself is stated.
    private static List<LoadUnit> RejectUnits(BatchCopyPipelineConfiguration configuration) =>
        [.. LoadPhaseRows.Rejecting(configuration.LoadSteps).Where(row => row.IsEnabled).OrderBy(row => row.StepOrder).Select(LoadUnit.ForStep)];

    private static IGenericResult<PipelineLoadPlan> ForSteps(
        BatchCopyPipelineConfiguration configuration,
        IList<PipelineLoadStepConfiguration> rows,
        ILogger logger)
    {
        if (configuration.TruncateBeforeLoad)
            return GenericResult<PipelineLoadPlan>.Failure(
                PipelineRunLog.TruncateBeforeLoadContradicts(logger, configuration.Name, "the pipeline states load steps"));

        var steps = rows
            .Where(row => row.StepOrder is not null && row.IsEnabled)
            .OrderBy(row => row.StepOrder)
            .Select(LoadUnit.ForStep)
            .ToList();
        return GenericResult<PipelineLoadPlan>.Success(new PipelineLoadPlan(null, steps, false, true, false, false, RejectUnits(configuration)));
    }

    private static IGenericResult<PipelineLoadPlan> ForOwnLoad(
        BatchCopyPipelineConfiguration configuration,
        PipelineLoadStepConfiguration? stated,
        ILogger logger)
    {
        var mode = LoadModes.ByName(configuration.LoadMode!);
        if (configuration.TruncateBeforeLoad && !mode.TruncatesFirst)
            return GenericResult<PipelineLoadPlan>.Failure(
                PipelineRunLog.TruncateBeforeLoadContradicts(logger, configuration.Name, string.Concat("load mode ", mode.Name)));

        var row = stated is null ? new PipelineLoadStepConfiguration() : stated;
        ILoadStepKind kind = mode.UsesKeyColumns ? LoadStepKinds.BulkUpsert : LoadStepKinds.BulkInsert;
        var flagsAbsent = !string.IsNullOrWhiteSpace(row.SoftDeleteMode);
        return GenericResult<PipelineLoadPlan>.Success(new PipelineLoadPlan(
            LoadUnit.ForOwnLoad(configuration, row, kind), [], mode.TruncatesFirst, false, mode.UsesKeyColumns, flagsAbsent, RejectUnits(configuration)));
    }
}
