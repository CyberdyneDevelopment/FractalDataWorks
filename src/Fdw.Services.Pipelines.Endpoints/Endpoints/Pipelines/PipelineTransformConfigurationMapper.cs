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
    /// <param name="pipelineType">The type of the pipeline the stages are for, as its pipelineType states it (for example BatchCopy); a stage whose kind does not run on it is refused.</param>
    /// <param name="specs">The ordered transform-operation specs from the request.</param>
    /// <param name="logger">The logger used for the log-and-return MessageLogging failure.</param>
    /// <returns>Success with one configuration per spec, or the first mapping failure.</returns>
    public static IGenericResult<List<PipelineTransformConfiguration>> Map(
        string pipelineType,
        IEnumerable<ITransformOperationSpec> specs,
        ILogger logger) =>
        Map(pipelineType, specs, logger, StageChains.None.Ids);

    /// <summary>
    /// Maps the stages a request states, resolving the Transformations their field mappings name through
    /// <paramref name="transformationIds"/> (name to id, from <see cref="StageChainLookup"/>).
    /// </summary>
    /// <param name="pipelineType">The type of the pipeline the stages are for, as its pipelineType states it (for example BatchCopy); a stage whose kind does not run on it is refused.</param>
    /// <param name="specs">The stages the request states.</param>
    /// <param name="logger">The logger refusals are written to.</param>
    /// <param name="transformationIds">The id of each Transformation the request names, by name.</param>
    /// <returns>The configuration of each stage, or the coded refusal naming the first stage that cannot be mapped.</returns>
    public static IGenericResult<List<PipelineTransformConfiguration>> Map(
        string pipelineType,
        IEnumerable<ITransformOperationSpec> specs,
        ILogger logger,
        IReadOnlyDictionary<string, Guid> transformationIds)
    {
        return MapStages(pipelineType, specs, logger, transformationIds, new HashSet<string>(StringComparer.OrdinalIgnoreCase), [], 0);
    }

    // Maps one list of stages: the stages of the pipeline, or the stages of one path of a Branch. The names set is shared by the
    // whole tree, so a name is used once however deep it sits; the parent path is the path of stage and path names down to this
    // list; the depth is how many Branch stages enclose it. The Transformation ids are looked up once for the whole request.
    private static IGenericResult<List<PipelineTransformConfiguration>> MapStages(
        string pipelineType,
        IEnumerable<ITransformOperationSpec> specs,
        ILogger logger,
        IReadOnlyDictionary<string, Guid> transformationIds,
        HashSet<string> names,
        IReadOnlyList<string> parentPath,
        int depth)
    {
        var results = new List<PipelineTransformConfiguration>();

        foreach (var spec in specs)
        {
            var stagePath = parentPath.Append(spec.Name).ToList();
            if (TransformTypes.ByName(spec.OperationType) is not TransformTypeBase option || option == TransformTypes.NotFound)
            {
                return Refused(spec, EtlLog.UnknownTransformType(logger, spec.OperationType).Message, logger, stagePath);
            }

            var problem = StageProblem(spec, option, names, pipelineType);
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

            if (option is ICompositeTransformType composite)
            {
                var nested = MapPaths(pipelineType, spec, config, composite, option, logger, transformationIds, names, stagePath, depth + 1);
                if (nested.IsFailure)
                {
                    return nested.ToNewResult<List<PipelineTransformConfiguration>>();
                }
            }

            var chained = AttachChains(spec, config, transformationIds, logger) ?? AttachCompute(spec, config, transformationIds, logger);
            if (chained is not null)
            {
                return Refused(spec, chained, logger, stagePath);
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

    // The chain each field mapping states, stored as rows that point at the Transformation by id. The mapping rows were built
    // from the spec's mappings one to one and in order (see the Map stage), so they pair up by position. A mapping has a
    // transform expression or a chain, never both; the steps' ordinals run from 1 without gaps; and every Transformation a step
    // points at was found by the lookup. Returns why a chain cannot be stored, or null.
    private static string? AttachChains(
        ITransformOperationSpec spec,
        PipelineTransformConfiguration config,
        IReadOnlyDictionary<string, Guid> transformationIds,
        ILogger logger)
    {
        for (var index = 0; index < spec.FieldMappings.Count && index < config.FieldMappings.Count; index++)
        {
            if (spec.FieldMappings[index] is not IFieldMappingChainSpec { TransformationReferences.Count: > 0 } chain)
            {
                continue;
            }

            var problem = ChainProblem(spec.FieldMappings[index], chain.TransformationReferences, config.FieldMappings[index], transformationIds, logger);
            if (problem is not null)
            {
                return problem;
            }
        }

        return null;
    }

    private static string? ChainProblem(
        IFieldMapping mapping,
        IReadOnlyList<IFieldTransformationReferenceSpec> steps,
        PipelineTransformFieldMappingConfiguration target,
        IReadOnlyDictionary<string, Guid> transformationIds,
        ILogger logger)
    {
        var mappingName = target.Name;
        if (!string.IsNullOrEmpty(mapping.TransformExpression))
        {
            return StageChainLog.ExpressionAndChain(logger, mappingName).Message;
        }

        var ordered = steps.OrderBy(step => step.Ordinal).ToList();
        if (!ordered.Select(step => step.Ordinal).SequenceEqual(Enumerable.Range(1, ordered.Count)))
        {
            return StageChainLog.OrdinalsNotConsecutive(logger, mappingName, ordered.Count).Message;
        }

        foreach (var step in ordered)
        {
            var name = string.IsNullOrWhiteSpace(step.Transformation) ? null : step.Transformation;
            var problem = ReferenceProblem(mappingName, step, name, transformationIds, logger, out var id);
            if (problem is not null)
            {
                return problem;
            }

            target.TransformationChain.Add(new PipelineTransformFieldMappingTransformationConfiguration
            {
                TransformationId = id,
                Ordinal = step.Ordinal,
            });
        }

        return null;
    }

    // What is wrong with one step's reference to a Transformation, or null; the id it points at comes back in id.
    private static string? ReferenceProblem(
        string mappingName,
        IFieldTransformationReferenceSpec step,
        string? name,
        IReadOnlyDictionary<string, Guid> transformationIds,
        ILogger logger,
        out Guid id)
    {
        id = Guid.Empty;
        if (name is not null && step.TransformationId is not null)
        {
            return StageChainLog.StepNamesBoth(logger, mappingName, step.Ordinal).Message;
        }

        if (name is null && step.TransformationId is null)
        {
            return StageChainLog.StepNamesNeither(logger, mappingName, step.Ordinal).Message;
        }

        if (step.TransformationId is { } given)
        {
            id = given;
            return null;
        }

        if (transformationIds.TryGetValue(name!, out var found))
        {
            id = found;
            return null;
        }

        return StageChainLog.TransformationNotFound(logger, mappingName, step.Ordinal, name!).Message;
    }

    // What a stage's name and members must be before its kind reads it: a name is not empty, has no '.' (a dotted name
    // is how a stage inside another is displayed) and is used once in the pipeline; and a member the stage's kind does not
    // own is refused, not ignored. A kind that does not run on the pipeline's type is refused first among those: the rest of its
    // body would be judged for a pipeline that cannot hold it.
    private static string? StageProblem(ITransformOperationSpec spec, TransformTypeBase option, HashSet<string> names, string pipelineType)
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

        var unsupported = PipelineTypeProblem(spec, option, pipelineType);
        return unsupported is null ? MemberProblem(spec, option) : unsupported;
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
        if (foreign is not null)
        {
            return $"'{foreign}' is not a member of a {spec.OperationType} stage";
        }

        var unavailable = stated.StatedMappingMembers.FirstOrDefault(member => !owned.Contains(member, StringComparer.OrdinalIgnoreCase));
        return unavailable is null ? null : $"'{unavailable}' on a field mapping of a {spec.OperationType} stage is not available yet";
    }

    // The kind's own list of the pipeline types it runs on decides; no pipeline type is named here.
    private static string? PipelineTypeProblem(ITransformOperationSpec spec, TransformTypeBase option, string pipelineType)
    {
        var runsOn = option.PipelineTypes;
        if (runsOn is null || runsOn.Contains(pipelineType, StringComparer.OrdinalIgnoreCase))
        {
            return null;
        }

        return $"a {spec.OperationType} stage cannot run on a {pipelineType} pipeline; it runs on {string.Join(", ", runsOn)} only";
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

    // Maps the stages of each path of a composite stage the way the stages of the pipeline are mapped, and gives them to the
    // path. The limit on how deep stages nest is read from the stage kind, not written here.
    private static IGenericResult<List<PipelineTransformConfiguration>> MapPaths(
        string pipelineType,
        ITransformOperationSpec spec,
        PipelineTransformConfiguration config,
        ICompositeTransformType composite,
        TransformTypeBase option,
        ILogger logger,
        IReadOnlyDictionary<string, Guid> transformationIds,
        HashSet<string> names,
        IReadOnlyList<string> stagePath,
        int depth)
    {
        if (option is INestedStageShape shape && depth > shape.MaxDepth)
        {
            return Refused(spec, $"a {spec.OperationType} stage nests more than {shape.MaxDepth} deep", logger, stagePath);
        }

        foreach (var path in composite.PathsOf(spec))
        {
            var pathPath = stagePath.Append(path.Name).ToList();
            var stages = MapStages(pipelineType, path.Stages, logger, transformationIds, names, pathPath, depth);
            if (stages.IsFailure)
            {
                return stages;
            }

            composite.Attach(config, path.Name, stages.Value!);
        }

        return GenericResult<List<PipelineTransformConfiguration>>.Success([]);
    }
}
