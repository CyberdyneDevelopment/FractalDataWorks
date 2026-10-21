using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions;
using Fdw.Services.Etl.Abstractions.OptionTypes;
using Fdw.Services.Etl.Logging;
using Fdw.Services.Etl.Results;
using Fdw.Services.Etl.Transforms;
using Fdw.Services.Pipelines.Endpoints.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// Maps <see cref="CreatePipelineTransformRequest"/> specs onto typed <see cref="PipelineTransformConfiguration"/>
/// aggregates for the create/update pipeline endpoints (FDW-556).
/// </summary>
/// <remarks>
/// Why: dispatches per-option mapping via <c>TransformTypes.ByName(spec.OperationType).MapSpecToConfiguration</c>
/// — never a switch/if-else on <see cref="ITransformOperationSpec.OperationType"/>. Each option validates
/// its own required parameters and fails loud (structured MessageLogging error, non-success result) when
/// they are absent, so a param-less combine op can never silently no-op at create/update time — mirroring
/// the runtime fail-loud already enforced by <c>TransformTypeBase.TransformBatch</c>. This is domain mapping
/// logic and therefore belongs in FDW (DI-ownership rule), not duplicated per application.
/// </remarks>
public static class PipelineTransformConfigurationMapper
{
    /// <summary>
    /// Maps every transform spec in <paramref name="specs"/> onto a typed <see cref="PipelineTransformConfiguration"/>,
    /// stopping at (and returning) the first failure.
    /// </summary>
    /// <param name="specs">The ordered transform-operation specs from the request.</param>
    /// <param name="logger">The logger used for the log-and-return MessageLogging failure.</param>
    /// <returns>Success with one configuration per spec, or the first mapping failure.</returns>
    public static IGenericResult<List<PipelineTransformConfiguration>> Map(
        IEnumerable<ITransformOperationSpec> specs,
        ILogger logger) =>
        Map(specs, logger, StageChains.None.Ids);

    /// <summary>
    /// Maps the stages a request states, resolving the Built transformation a Compute stage names through
    /// <paramref name="transformationIds"/> (name to id, from <see cref="StageChainLookup"/>).
    /// </summary>
    /// <param name="specs">The stages the request states.</param>
    /// <param name="logger">The logger refusals are written to.</param>
    /// <param name="transformationIds">The id of each Transformation the request names, by name.</param>
    /// <returns>The configuration of each stage, or the coded refusal naming the first stage that cannot be mapped.</returns>
    public static IGenericResult<List<PipelineTransformConfiguration>> Map(
        IEnumerable<ITransformOperationSpec> specs,
        ILogger logger,
        IReadOnlyDictionary<string, Guid> transformationIds)
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var results = new List<PipelineTransformConfiguration>();

        foreach (var spec in specs)
        {
            IReadOnlyList<string> stagePath = [spec.Name];
            if (TransformTypes.ByName(spec.OperationType) is not TransformTypeBase option || option == TransformTypes.NotFound)
            {
                return Refused(spec, EtlLog.UnknownTransformType(logger, spec.OperationType).Message, logger, stagePath);
            }

            var problem = StageProblem(spec, option, names);
            if (problem is not null)
            {
                return Refused(spec, problem, logger, stagePath);
            }

            var config = new PipelineTransformConfiguration
            {
                Id = Guid.CreateVersion7(),
                Name = spec.Name,
                OperationType = spec.OperationType,
                ExecutionOrder = spec.ExecutionOrder,
                IsEnabled = spec.IsEnabled,
            };

            var mapResult = option.MapSpecToConfiguration(spec, config, logger);
            if (!mapResult.IsSuccess)
            {
                return Refused(spec, mapResult.CurrentMessage, logger, stagePath);
            }

            var computed = AttachCompute(spec, config, transformationIds, logger);
            if (computed is not null)
            {
                return Refused(spec, computed, logger, stagePath);
            }

            results.Add(config);
        }

        return GenericResult<List<PipelineTransformConfiguration>>.Success(results);
    }

    // A Compute stage that names its Built transformation gets the id the lookup found for that name; one that gives the id
    // already has it. A name the lookup did not find means the mapper was used without the lookup, so it refuses rather than
    // storing a stage that points at nothing. Returns why the stage cannot be stored, or null.
    private static string? AttachCompute(
        ITransformOperationSpec spec,
        PipelineTransformConfiguration config,
        IReadOnlyDictionary<string, Guid> transformationIds,
        ILogger logger)
    {
        if (spec is not ITransformComputeSpec { ComputeTransformation: { Length: > 0 } name } || config.ComputeTransformationId is not null)
        {
            return null;
        }

        if (!transformationIds.TryGetValue(name, out var id))
        {
            return ComputeStageLog.TransformationNotFound(logger, name).Message;
        }

        config.ComputeTransformationId = id;
        return null;
    }

    // What a stage's name and members must be before its kind reads it: a name is not empty, has no '.' (a dotted name
    // is how a stage inside another is displayed) and is used once in the pipeline; and a member the stage's kind does not
    // own is refused, not ignored.
    private static string? StageProblem(ITransformOperationSpec spec, TransformTypeBase option, HashSet<string> names)
    {
        if (string.IsNullOrWhiteSpace(spec.Name))
        {
            return "every stage is named";
        }

        if (spec.Name.Contains('.', StringComparison.Ordinal))
        {
            return $"the stage name '{spec.Name}' cannot contain '.'";
        }

        if (!names.Add(spec.Name))
        {
            return $"the stage name '{spec.Name}' is used by more than one stage";
        }

        return MemberProblem(spec, option);
    }

    // A member the stage states that its kind does not own is refused, not ignored.
    private static string? MemberProblem(ITransformOperationSpec spec, TransformTypeBase option)
    {
        if (spec is not IStatedMembersSpec stated)
        {
            return null;
        }

        var owned = option.Members.Select(member => member.Name).ToList();
        var foreign = stated.StatedMembers.FirstOrDefault(member => !owned.Contains(member, StringComparer.OrdinalIgnoreCase));
        return foreign is null ? null : $"'{foreign}' is not a member of a {spec.OperationType} stage";
    }

    // Why a coded refusal and not the step option's own failure: the option logs under an EtlLog event
    // number (11xxx), which ResultHttpStatusMapper reads as a non-failure band, so a refused step used to
    // reach the caller as a 500 UNKNOWN_ERROR. A step the caller got wrong is validation input (20001),
    // answered 400 with the step's name and the reason the option gave.
    private static IGenericResult<List<PipelineTransformConfiguration>> Refused(
        ITransformOperationSpec spec, string? reason, ILogger logger, IReadOnlyList<string> stagePath)
    {
        return GenericResult<List<PipelineTransformConfiguration>>.Failure(
            EtlResultCodes.ByName("InvalidPipelineStep"),
            logger,
            ResultDetails.Create(
                "StepName", string.Join('.', stagePath),
                "Reason", reason,
                PipelineStagePath.DetailKey, PipelineStagePath.Join(stagePath)));
    }
}
