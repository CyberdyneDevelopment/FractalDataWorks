using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Services.Connections.Abstractions;
using Fdw.Results;
using Fdw.Services.Data;
using Fdw.Services.Etl.Abstractions.OptionTypes;
using Fdw.Services.Etl.Pipelines.Loads;
using Fdw.Services.Pipelines.Clients.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// Judges a pipeline body's load definition: maps it, validates its structure with the engine's own
/// <see cref="PipelineLoadValidator"/>, and checks it against the data sets and data stores it touches
/// (<see cref="PipelineLoadSchemaCheck"/>). Create and update take the first refusal; validate lists every problem, each
/// on the stage -- the load step's name -- it belongs to (FDW-1110, contract item 6).
/// </summary>
public sealed class PipelineLoadDraft
{
    private readonly PipelineLoadSchemaCheck _schema;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="PipelineLoadDraft"/> class.</summary>
    /// <param name="dataSets">The data set configurations the columns are read from.</param>
    /// <param name="dataStores">The data store configurations an Execute step's container is read from.</param>
    /// <param name="connections">The connection configurations an Execute step's connection type is read from.</param>
    /// <param name="logger">The logger the refusals are written to.</param>
    public PipelineLoadDraft(
        IDataSetConfigurationProvider dataSets, DataStoreConfigurationProvider dataStores, IConnectionConfigurationProvider connections, ILogger logger)
    {
        _schema = new PipelineLoadSchemaCheck(dataSets, dataStores, connections, logger);
        _logger = logger;
    }

    /// <summary>Maps and judges a load definition for a create or an update.</summary>
    /// <param name="load">The definition as the body states it.</param>
    /// <param name="pipelineDestinationDataSet">The pipeline's destination data set.</param>
    /// <param name="expressions">The filter expressions of the pipeline's transforms, each with the stage it sits on.</param>
    /// <param name="cancellationToken">Cancels the check.</param>
    /// <returns>The mapped definition, or the first refusal.</returns>
    public async Task<IGenericResult<MappedPipelineLoad>> Prepare(
        IPipelineLoadFields load,
        string? pipelineDestinationDataSet,
        IReadOnlyList<(string? Stage, string? Expression)> expressions,
        CancellationToken cancellationToken = default)
    {
        var mapped = Map(load);
        if (mapped.IsFailure)
            return mapped;

        return await Judge(mapped.Value!, pipelineDestinationDataSet, expressions, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Maps a load definition as a body states it, answering a refusal with the reason (FDW-1147).</summary>
    /// <param name="load">The definition as the body states it.</param>
    /// <returns>The mapped definition, or the refusal.</returns>
    public IGenericResult<MappedPipelineLoad> Map(IPipelineLoadFields load)
    {
        var mapped = PipelineLoadMapper.Map(load, _logger);
        return mapped.IsFailure ? PipelineLoadRefusal.From<MappedPipelineLoad>(mapped) : mapped;
    }

    /// <summary>Judges an already mapped load definition: the stored one with a change applied, for an update.</summary>
    /// <param name="mapped">The definition as configuration rows.</param>
    /// <param name="pipelineDestinationDataSet">The pipeline's destination data set.</param>
    /// <param name="expressions">The filter expressions of the pipeline's transforms, each with the stage it sits on.</param>
    /// <param name="cancellationToken">Cancels the check.</param>
    /// <returns>The definition, or the first refusal.</returns>
    public async Task<IGenericResult<MappedPipelineLoad>> Judge(
        MappedPipelineLoad mapped,
        string? pipelineDestinationDataSet,
        IReadOnlyList<(string? Stage, string? Expression)> expressions,
        CancellationToken cancellationToken = default)
    {
        var structure = PipelineLoadValidator.Validate(mapped.LoadMode, mapped.LoadSteps, mapped.RunParameters, pipelineDestinationDataSet, _logger);
        if (structure.IsFailure)
            return PipelineLoadRefusal.From<MappedPipelineLoad>(structure);

        var problems = await _schema.Check(mapped, pipelineDestinationDataSet, expressions, cancellationToken).ConfigureAwait(false);
        return problems.Count == 0
            ? GenericResult<MappedPipelineLoad>.Success(mapped)
            : PipelineLoadRefusal.Logged<MappedPipelineLoad>(Reason(problems[0]), _logger);
    }

    // The reason names the stage and the field the first problem is on, as the validate endpoints place it, then the problem.
    private static string Reason(PipelineProblem problem) =>
        string.Concat(
            problem.Stage is { } stage ? $"at stage '{stage}'" : "at the pipeline",
            string.IsNullOrEmpty(problem.Field) ? string.Empty : $", {problem.Field}",
            ": ",
            problem.Reason);

    /// <summary>Lists every problem of a load definition, for the validate endpoints.</summary>
    /// <param name="load">The definition as the body states it.</param>
    /// <param name="pipelineDestinationDataSet">The pipeline's destination data set.</param>
    /// <param name="expressions">The filter expressions of the pipeline's transforms, each with the stage it sits on.</param>
    /// <param name="cancellationToken">Cancels the check.</param>
    /// <returns>The problems; empty when create would accept the definition.</returns>
    public async Task<IReadOnlyList<PipelineProblem>> Problems(
        IPipelineLoadFields load,
        string? pipelineDestinationDataSet,
        IReadOnlyList<(string? Stage, string? Expression)> expressions,
        CancellationToken cancellationToken = default)
    {
        var mapped = PipelineLoadMapper.Map(load, _logger);
        if (mapped.IsFailure)
            return [Own("LoadSteps", mapped.CurrentMessage!)];

        var problems = StructureProblems(load, mapped.Value!, pipelineDestinationDataSet);
        if (problems.Count > 0)
            return problems;

        return await _schema.Check(mapped.Value!, pipelineDestinationDataSet, expressions, cancellationToken).ConfigureAwait(false);
    }

    // Each step is validated alone, as the only step, so a refusal is placed on the step it is about; a refusal
    // that only the whole definition has (a repeated name, an own load beside steps) is the pipeline's. A reject step is
    // judged alone against the rows that load the output, whose names and destinations it may not reuse.
    private List<PipelineProblem> StructureProblems(IPipelineLoadFields load, MappedPipelineLoad mapped, string? pipelineDestinationDataSet)
    {
        var loading = LoadPhaseRows.Loading(mapped.LoadSteps);
        List<PipelineProblem> problems =
        [
            .. RefusedSteps(load.LoadSteps, step => PipelineLoadValidator.Validate(null, [PipelineLoadMapper.MapStep(step, 1, LoadPhases.Load)], null, null, _logger)),
            .. RefusedSteps(load.RejectSteps, step => PipelineRejectStepValidator.Validate([PipelineLoadMapper.MapStep(step, 1, LoadPhases.Reject)], [.. loading], pipelineDestinationDataSet, _logger)),
        ];
        if (problems.Count > 0)
            return problems;

        var whole = PipelineLoadValidator.Validate(mapped.LoadMode, mapped.LoadSteps, mapped.RunParameters, pipelineDestinationDataSet, _logger);
        if (whole.IsFailure)
            problems.Add(Own(mapped.LoadSteps is null ? "LoadMode" : "LoadSteps", whole.CurrentMessage!));
        return problems;
    }

    private static List<PipelineProblem> RefusedSteps(IList<PipelineLoadStep>? steps, Func<PipelineLoadStep, IGenericResult> judge)
    {
        var problems = new List<PipelineProblem>();
        foreach (var step in steps is null ? [] : steps)
        {
            var judged = judge(step);
            if (judged.IsFailure)
                problems.Add(new PipelineProblem { Stage = step.Name, StagePath = [step.Name], Field = "Options", Reason = judged.CurrentMessage! });
        }

        return problems;
    }

    private static PipelineProblem Own(string field, string reason) => new() { Stage = null, Field = field, Reason = reason };
}
