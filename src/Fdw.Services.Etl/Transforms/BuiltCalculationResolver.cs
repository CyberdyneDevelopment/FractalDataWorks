using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.Transformations;
using Fdw.Data.Transformations.Configuration;
using Fdw.Results;
using Fdw.Services.Calculations.Abstractions.Runtime;
using Fdw.Services.Calculations.Abstractions.Sets;
using Fdw.Services.Calculations.Abstractions.Trees;
using Fdw.Services.Calculations.Compilation;
using Fdw.Services.Etl.Logging;
using Fdw.Services.Settings;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Transforms;

/// <summary>
/// Turns the Built Transformations that the Compute stages of a pipeline name into compiled calculations, once, when the run
/// starts.
/// </summary>
/// <remarks>
/// Why the run resolves them up front, as it does the chains of field mappings: a stage runs per record and cannot reach the
/// Transformation domain, and a Transformation that no longer resolves, is not a Built transformation, leaves an input
/// unmapped or does not compile fails the run before any record moves, naming the stage. Nothing is skipped and no limit is
/// assumed: the three calculation limits come from settings the platform states, and a missing one fails the run.
/// The stages inside the paths of a Branch are searched too, however deeply they nest. A disabled stage is not searched,
/// because it never runs.
/// </remarks>
public static class BuiltCalculationResolver
{
    /// <summary>The operation name of a Compute stage.</summary>
    public const string ComputeOperation = "Compute";

    /// <summary>Resolves and compiles the calculations of the Compute stages of <paramref name="stages"/>.</summary>
    /// <param name="stages">The stages searched, and the stages inside their paths; null for a pipeline with no stages.</param>
    /// <param name="transformations">The Transformation provider; needed only when some stage is a Compute stage.</param>
    /// <param name="settings">The settings provider the calculation limits are read from; needed only when some stage is a Compute stage.</param>
    /// <param name="logger">The logger the refusals are written to.</param>
    /// <param name="cancellationToken">Cancels the resolution.</param>
    /// <returns>The compiled calculations by stage id (empty when no stage is a Compute stage), or the failure naming the stage.</returns>
    public static async Task<IGenericResult<BuiltCompilation>> Resolve(
        IEnumerable<PipelineTransformConfiguration>? stages,
        ITransformationConfigurationProvider? transformations,
        IEffectiveSettingsProvider? settings,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        var resolution = new BuiltCalculationResolution(transformations, settings, logger);
        var failure = await ResolveStages(stages, null, resolution, cancellationToken).ConfigureAwait(false);
        if (failure is not null)
        {
            return failure.ToNewResult<BuiltCompilation>();
        }

        ComputeLog.CalculationsCompiled(logger, resolution.Compiled.Count + resolution.CompiledSets.Count);
        return GenericResult<BuiltCompilation>.Success(new BuiltCompilation(resolution.Compiled, resolution.CompiledSets));
    }

    private static async Task<IGenericResult?> ResolveStages(
        IEnumerable<PipelineTransformConfiguration>? stages,
        string? parentName,
        BuiltCalculationResolution resolution,
        CancellationToken cancellationToken)
    {
        if (stages is null)
        {
            return null;
        }

        foreach (var stage in stages.Where(candidate => candidate.IsEnabled))
        {
            var stageName = parentName is null ? stage.Name : string.Concat(parentName, ".", stage.Name);
            if (string.Equals(stage.OperationType, ComputeOperation, StringComparison.OrdinalIgnoreCase))
            {
                var resolved = await ResolveStage(stageName, stage, resolution, cancellationToken).ConfigureAwait(false);
                if (resolved.IsFailure)
                {
                    return resolved;
                }
            }
        }

        return null;
    }

    private static async Task<IGenericResult> ResolveStage(
        string stageName,
        PipelineTransformConfiguration stage,
        BuiltCalculationResolution resolution,
        CancellationToken cancellationToken)
    {
        var built = await FindBuilt(stageName, stage, resolution, cancellationToken).ConfigureAwait(false);
        if (built.IsFailure)
        {
            return built;
        }

        if (built.Value!.TableOutputs.Count > 0)
        {
            return BuiltSetResolver.Resolve(stageName, stage, built.Value!, resolution);
        }

        var members = BuiltSetResolver.CheckRecordMode(stageName, stage, built.Value!, resolution.Logger);
        if (members.IsFailure)
        {
            return members;
        }

        var inputs = CheckInputs(stageName, stage, built.Value!, resolution.Logger);
        if (inputs.IsFailure)
        {
            return inputs;
        }

        var limits = ReadLimits(stageName, resolution);
        if (limits.IsFailure)
        {
            return limits;
        }

        var array = BuiltTransformationConverter.ToArray(built.Value!, limits.Value!);
        var compiled = array.IsFailure
            ? array.ToNewResult<CompiledCalculation>()
            : new CalculationCompiler().Compile(array.Value!, limits.Value!);
        if (compiled.IsFailure)
        {
            ComputeLog.CalculationRefused(resolution.Logger, stageName, built.Value!.Name);
            return compiled;
        }

        resolution.Compiled[stage.Id] = compiled.Value!;
        return GenericResult.Success();
    }

    private static async Task<IGenericResult<BuiltTransformationConfiguration>> FindBuilt(
        string stageName,
        PipelineTransformConfiguration stage,
        BuiltCalculationResolution resolution,
        CancellationToken cancellationToken)
    {
        if (stage.ComputeTransformationId is not { } id)
        {
            return GenericResult<BuiltTransformationConfiguration>.Failure(ComputeLog.TransformationNotStated(resolution.Logger, stageName));
        }

        if (resolution.Transformations is null)
        {
            return GenericResult<BuiltTransformationConfiguration>.Failure(ComputeLog.TransformationProviderMissing(resolution.Logger, stageName));
        }

        var found = await resolution.Transformations.Get(id, cancellationToken).ConfigureAwait(false);
        if (found.IsFailure || found.Value is null)
        {
            return GenericResult<BuiltTransformationConfiguration>.Failure(ComputeLog.TransformationMissing(resolution.Logger, stageName, id));
        }

        return found.Value is BuiltTransformationConfiguration built
            ? GenericResult<BuiltTransformationConfiguration>.Success(built)
            : GenericResult<BuiltTransformationConfiguration>.Failure(
                ComputeLog.TransformationNotBuilt(resolution.Logger, stageName, found.Value.Name, found.Value.Implementation));
    }

    // Every input the transformation declares must have a field mapped to it, and every mapping must name a declared input.
    private static IGenericResult CheckInputs(
        string stageName,
        PipelineTransformConfiguration stage,
        BuiltTransformationConfiguration built,
        ILogger logger)
    {
        foreach (var declared in built.Inputs)
        {
            if (!stage.ComputeInputs.Any(mapped => string.Equals(mapped.InputName, declared.Name, StringComparison.Ordinal)))
            {
                return GenericResult.Failure(ComputeLog.InputNotMapped(logger, stageName, declared.Name, built.Name));
            }
        }

        foreach (var mapped in stage.ComputeInputs)
        {
            if (!built.Inputs.Any(declared => string.Equals(declared.Name, mapped.InputName, StringComparison.Ordinal)))
            {
                return GenericResult.Failure(ComputeLog.InputNotDeclared(logger, stageName, mapped.InputName, built.Name));
            }
        }

        return GenericResult.Success();
    }

    // The limits are stated by the platform's settings. They are read once per run, the first time a Compute stage needs them.
    internal static IGenericResult<CalculationLimits> ReadLimits(string stageName, BuiltCalculationResolution resolution)
    {
        if (resolution.Limits is not null)
        {
            return GenericResult<CalculationLimits>.Success(resolution.Limits);
        }

        if (resolution.Settings is null)
        {
            return GenericResult<CalculationLimits>.Failure(ComputeLog.SettingsProviderMissing(resolution.Logger, stageName));
        }

        var read = CalculationLimitSettings.Read(resolution.Settings, resolution.Logger);
        if (read.IsSuccess)
        {
            resolution.Limits = read.Value;
        }

        return read;
    }

    // The limits on the rows of a calculation set are read like the others: once per run, from settings the platform states.
    internal static IGenericResult<SetLimits> ReadSetLimits(string stageName, BuiltCalculationResolution resolution)
    {
        if (resolution.SetLimits is not null)
        {
            return GenericResult<SetLimits>.Success(resolution.SetLimits);
        }

        if (resolution.Settings is null)
        {
            return GenericResult<SetLimits>.Failure(ComputeLog.SettingsProviderMissing(resolution.Logger, stageName));
        }

        var read = CalculationLimitSettings.ReadSet(resolution.Settings, resolution.Logger);
        if (read.IsSuccess)
        {
            resolution.SetLimits = read.Value;
        }

        return read;
    }
}
