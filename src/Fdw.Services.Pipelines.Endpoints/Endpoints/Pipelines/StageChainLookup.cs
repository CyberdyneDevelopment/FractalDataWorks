using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.Transformations.Configuration;
using Fdw.Results;
using Fdw.Services.Pipelines.Clients.Abstractions;
using Fdw.Services.Pipelines.Endpoints.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// Looks up the named Transformations that the mappings on the inputs of a graph's components chain together, and reports every problem
/// with a chain, each on its component, without saving anything.
/// </summary>
/// <remarks>
/// Why this is one check shared by create, update and the two validate endpoints: they must refuse exactly what saving
/// refuses, so they all ask the same code. Why the lookup resolves names to ids here and the mapper stores the ids: a
/// relationship is a configured key and a name can be renamed; the request may name a Transformation or give its id, and
/// the pipeline keeps the id.
/// </remarks>
public sealed class StageChainLookup
{
    private readonly ITransformationConfigurationProvider _transformations;
    private readonly ILogger _logger;
    private readonly StageComputeCheck _compute;

    /// <summary>
    /// Initializes a new instance of the <see cref="StageChainLookup"/> class.
    /// </summary>
    /// <param name="transformations">The Transformation provider the names and ids are looked up in.</param>
    /// <param name="logger">The logger the problems are written to.</param>
    public StageChainLookup(ITransformationConfigurationProvider transformations, ILogger logger)
    {
        _transformations = transformations;
        _logger = logger;
        _compute = new StageComputeCheck(transformations, logger);
    }

    /// <summary>
    /// Looks up every chain stated by <paramref name="maps"/>, and checks the Compute of <paramref name="stages"/>.
    /// </summary>
    /// <param name="pipelineName">The name of the pipeline the request is for, used to log the outcome.</param>
    /// <param name="stages">The stages the request states; null when it states none.</param>
    /// <param name="maps">The chains the mappings on the request's component inputs state; null when it states none.</param>
    /// <param name="cancellationToken">Cancels the lookup.</param>
    /// <returns>The ids of the Transformations the request names, and the problems found.</returns>
    public async Task<StageChains> Resolve(
        string pipelineName,
        IList<CreatePipelineTransformRequest>? stages,
        IReadOnlyList<MapChain>? maps,
        CancellationToken cancellationToken = default)
    {
        var found = new Found();
        await LookIn(stages, [], "Transforms", found, cancellationToken).ConfigureAwait(false);
        foreach (var map in maps ?? [])
        {
            await LookAtMap(map, found, cancellationToken).ConfigureAwait(false);
        }

        StageChainLog.ChainsLookedUp(_logger, pipelineName, found.Steps, found.Problems.Count);
        return new StageChains(found.Ids, found.Problems);
    }

    // Looks through one list of stages. The names are the stage names down to this list, which place a problem; the positions say
    // the same by index, for a stage that has no name yet.
    private async Task LookIn(
        IList<CreatePipelineTransformRequest>? stages,
        IReadOnlyList<string> names,
        string positions,
        Found found,
        CancellationToken cancellationToken)
    {
        for (var stageIndex = 0; stages is not null && stageIndex < stages.Count; stageIndex++)
        {
            var stage = stages[stageIndex];
            var stageNames = names.Append(stage.Name).ToList();
            var stagePositions = $"{positions}[{stageIndex}]";
            if (stage.Compute is not null)
            {
                await _compute.Check(stageNames, stagePositions, stage.Compute, found.Ids, found.Problems, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    // The chain one mapping on a component input states. A mapping that states none has nothing to look up.
    private async Task LookAtMap(MapChain map, Found found, CancellationToken cancellationToken)
    {
        if (map.Chain.Count == 0)
        {
            return;
        }

        found.Steps += map.Chain.Count;
        await CheckMapping(new ChainAt(map), map.Chain, found, cancellationToken).ConfigureAwait(false);
    }

    private async Task CheckMapping(
        ChainAt at,
        IList<PipelineFieldTransformationReference> chain,
        Found found,
        CancellationToken cancellationToken)
    {
        var mapping = at.Mapping;
        if (!chain.Select(step => step.Ordinal).OrderBy(ordinal => ordinal).SequenceEqual(Enumerable.Range(1, chain.Count)))
        {
            found.Problems.Add(at.Problem("Transformations", StageChainLog.OrdinalsNotConsecutive(_logger, mapping.Target, chain.Count).Message));
        }

        for (var stepIndex = 0; stepIndex < chain.Count; stepIndex++)
        {
            await CheckStep(at, stepIndex, chain[stepIndex], found, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task CheckStep(
        ChainAt at,
        int stepIndex,
        PipelineFieldTransformationReference step,
        Found found,
        CancellationToken cancellationToken)
    {
        var name = string.IsNullOrWhiteSpace(step.Transformation) ? null : step.Transformation;
        var field = StepField(stepIndex, "Transformation");
        var shape = ReferenceShapeProblem(at.Mapping.Target, step, name);
        if (shape is not null)
        {
            found.Problems.Add(at.Problem(field, shape));
            return;
        }

        if (name is not null && found.Ids.ContainsKey(name))
        {
            return;
        }

        var looked = await Find(name, step.TransformationId, cancellationToken).ConfigureAwait(false);
        var problem = FoundProblem(at.Mapping.Target, step, name, looked);
        if (problem is not null)
        {
            found.Problems.Add(at.Problem(field, problem));
            return;
        }

        if (name is not null && looked.IsSuccess)
        {
            found.Ids[name] = looked.Value!.Id;
        }
    }

    // A step names its Transformation or gives its id, one of the two.
    private string? ReferenceShapeProblem(string mappingName, PipelineFieldTransformationReference step, string? name)
    {
        if (name is not null && step.TransformationId is not null)
        {
            return StageChainLog.StepNamesBoth(_logger, mappingName, step.Ordinal).Message;
        }

        return name is null && step.TransformationId is null
            ? StageChainLog.StepNamesNeither(_logger, mappingName, step.Ordinal).Message
            : null;
    }

    private Task<IGenericResult<ITransformationImplementationConfiguration>> Find(string? name, Guid? id, CancellationToken cancellationToken) =>
        name is not null
            ? _transformations.Get(name, cancellationToken)
            : _transformations.Get(id!.Value, cancellationToken);

    // What is wrong with the Transformation a step points at: it does not exist, or it does not work on a single field's value.
    private string? FoundProblem(
        string mappingName,
        PipelineFieldTransformationReference step,
        string? name,
        IGenericResult<ITransformationImplementationConfiguration> found)
    {
        if (found.IsFailure || found.Value is null)
        {
            return name is not null
                ? StageChainLog.TransformationNotFound(_logger, mappingName, step.Ordinal, name).Message
                : StageChainLog.TransformationIdNotFound(_logger, mappingName, step.Ordinal, step.TransformationId!.Value).Message;
        }

        return found.Value is IFieldTransformationConfiguration
            ? null
            : StageChainLog.TransformationNotFieldLevel(_logger, mappingName, step.Ordinal, found.Value.Name, found.Value.Implementation).Message;
    }

    private static string StepField(int stepIndex, string member) => $"Transformations[{stepIndex}].{member}";

    // What a lookup finds as it goes: the ids of the Transformations the request names, the problems, and the steps it looked at.
    private sealed class Found
    {
        public Dictionary<string, Guid> Ids { get; } = new(StringComparer.OrdinalIgnoreCase);

        public List<PipelineProblem> Problems { get; } = [];

        public int Steps { get; set; }
    }

    // Where a chain sits in the request, so each problem is placed on its component and names the mapping and step it is about.
    private sealed class ChainAt(MapChain mapping)
    {
        public MapChain Mapping { get; } = mapping;

        public PipelineProblem Problem(string field, string? reason) =>
            PipelineProblems.OfStage([Mapping.Component], $"Maps[{Mapping.Index}].{field}", reason ?? string.Empty);
    }
}
