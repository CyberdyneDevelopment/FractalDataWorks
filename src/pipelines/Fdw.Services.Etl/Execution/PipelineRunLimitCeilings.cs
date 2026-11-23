using System;
using Fdw.Results;
using Fdw.Services.Etl.Logging;
using Fdw.Services.Settings;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Execution;

/// <summary>
/// The ceilings in force on a run's time and steps (FDW-6141): the most a pipeline may state, read from the settings when a run starts.
/// </summary>
/// <param name="MaxRunSeconds">The most seconds a pipeline may state.</param>
/// <param name="MaxSteps">The most steps a pipeline may state.</param>
/// <remarks>
/// The ceilings belong to the Interactive pipeline type and are judged only for a pipeline of that type (an ETL Graph pipeline may rightly state hours): a Graph pipeline's own
/// stated limits have no ceiling unless its type declares one, and a Graph child started at once is bounded by its parent's limits (<c>PipelineRunTimeAndStepLimits.ForChild</c>).
/// A ceiling is a setting, never a clamp: a row above it is refused at the save, and a stored value above the ceiling in force now (someone lowered the setting after the row was
/// saved) refuses the run, with a code naming the pipeline, the value and the ceiling; a run is never started with a silently lowered limit.
/// </remarks>
public sealed record PipelineRunLimitCeilings(int MaxRunSeconds, int MaxSteps)
{
    /// <summary>Reads the ceilings in force for a run's tenant from the settings.</summary>
    /// <param name="settings">The settings the ceilings are resolved from.</param>
    /// <param name="tenantId">The tenant the run belongs to.</param>
    /// <param name="logger">The logger a refusal is written to.</param>
    /// <returns>The ceilings, or the refusal naming the first setting that is not a positive number.</returns>
    public static IGenericResult<PipelineRunLimitCeilings> Read(IEffectiveSettingsProvider settings, Guid tenantId, ILogger logger)
    {
        var runSeconds = settings.GetEffectiveValue<int>(SettingDefinitions.InteractiveMaxRunSecondsCeiling, tenantId);
        if (runSeconds <= 0)
            return GenericResult<PipelineRunLimitCeilings>.Failure(PipelineRunLog.AtOnceCeilingNotSet(logger, SettingDefinitions.InteractiveMaxRunSecondsCeiling));

        var steps = settings.GetEffectiveValue<int>(SettingDefinitions.InteractiveMaxStepsCeiling, tenantId);
        return steps <= 0
            ? GenericResult<PipelineRunLimitCeilings>.Failure(PipelineRunLog.AtOnceCeilingNotSet(logger, SettingDefinitions.InteractiveMaxStepsCeiling))
            : GenericResult<PipelineRunLimitCeilings>.Success(new PipelineRunLimitCeilings(runSeconds, steps));
    }

    /// <summary>Judges what a pipeline states against the ceilings in force.</summary>
    /// <param name="pipelineName">The pipeline.</param>
    /// <param name="maxRunSeconds">The time limit the pipeline states, or null when it states none.</param>
    /// <param name="maxSteps">The step limit the pipeline states, or null when it states none.</param>
    /// <param name="ceilings">The ceilings in force.</param>
    /// <param name="logger">The logger.</param>
    /// <returns>Success, or a failure for each value above its ceiling.</returns>
    public static IGenericResult Judge(string pipelineName, int? maxRunSeconds, int? maxSteps, PipelineRunLimitCeilings ceilings, ILogger logger)
    {
        var refusals = new System.Collections.Generic.List<Fdw.Messages.IGenericMessage>();
        if (maxRunSeconds is { } seconds && seconds > ceilings.MaxRunSeconds)
            refusals.Add(PipelineRunLog.LimitAboveCeiling(logger, pipelineName, "MaxRunSeconds", seconds, ceilings.MaxRunSeconds));

        if (maxSteps is { } steps && steps > ceilings.MaxSteps)
            refusals.Add(PipelineRunLog.LimitAboveCeiling(logger, pipelineName, "MaxSteps", steps, ceilings.MaxSteps));

        return refusals.Count == 0 ? GenericResult.Success() : GenericResult.Failure(refusals);
    }
}
