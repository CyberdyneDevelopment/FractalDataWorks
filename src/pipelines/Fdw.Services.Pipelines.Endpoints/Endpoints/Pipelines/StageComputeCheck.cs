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
/// Checks a Compute stage a request states before the pipeline is saved: its transformation exists and is a Built
/// transformation, and its input mapping covers exactly the inputs that transformation declares.
/// </summary>
/// <remarks>
/// Why at save time as well as at run start: a mistake the caller can fix is refused where the caller reads it, on the stage
/// and the field, instead of failing a run later. The run still resolves and checks everything when it starts, because a
/// transformation can change after the pipeline was saved. A transformation named by name is resolved to its id here; the
/// mapper stores the id.
/// </remarks>
public sealed class StageComputeCheck
{
    private readonly ITransformationConfigurationProvider _transformations;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="StageComputeCheck"/> class.</summary>
    /// <param name="transformations">Resolves the Built transformation a Compute stage applies.</param>
    /// <param name="logger">The logger the refusals are written to.</param>
    public StageComputeCheck(ITransformationConfigurationProvider transformations, ILogger logger)
    {
        _transformations = transformations;
        _logger = logger;
    }

    /// <summary>Checks one Compute stage and adds what it finds to <paramref name="ids"/> and <paramref name="problems"/>.</summary>
    /// <param name="stageNames">The stage and path names down to this stage, which place a problem.</param>
    /// <param name="stagePositions">The position of the stage in the request, for a stage that has no name yet.</param>
    /// <param name="compute">What the stage states.</param>
    /// <param name="ids">The ids of the transformations the request names, by name; a name found here is added.</param>
    /// <param name="problems">The problems found; this stage's are added.</param>
    /// <param name="cancellationToken">Cancels the check.</param>
    /// <returns>A task that completes when the stage has been checked.</returns>
    public async Task Check(
        IReadOnlyList<string> stageNames,
        string stagePositions,
        PipelineCompute compute,
        Dictionary<string, Guid> ids,
        List<PipelineProblem> problems,
        CancellationToken cancellationToken = default)
    {
        var name = string.IsNullOrWhiteSpace(compute.Transformation) ? null : compute.Transformation;
        var shape = ShapeProblem(compute, name);
        if (shape is not null)
        {
            problems.Add(Problem(stageNames, stagePositions, "Compute.Transformation", shape));
            return;
        }

        var found = name is not null
            ? await _transformations.Get(name, cancellationToken).ConfigureAwait(false)
            : await _transformations.Get(compute.TransformationId!.Value, cancellationToken).ConfigureAwait(false);
        if (found.IsFailure || found.Value is null)
        {
            var missing = name is not null
                ? ComputeStageLog.TransformationNotFound(_logger, name).Message
                : ComputeStageLog.TransformationIdNotFound(_logger, compute.TransformationId!.Value).Message;
            problems.Add(Problem(stageNames, stagePositions, "Compute.Transformation", missing));
            return;
        }

        if (found.Value is not BuiltTransformationConfiguration built)
        {
            problems.Add(Problem(stageNames, stagePositions, "Compute.Transformation",
                ComputeStageLog.TransformationNotBuilt(_logger, found.Value.Name, found.Value.Implementation).Message));
            return;
        }

        if (name is not null)
        {
            ids[name] = built.Id;
        }

        foreach (var (member, reason) in ModeProblems(compute, built))
        {
            problems.Add(Problem(stageNames, stagePositions, member, reason));
        }
    }

    // A transformation that writes tables is checked for its table bindings, output routes and single values; one that reads a record at
    // a time for its field inputs, and none of the table members may be stated.
    private List<(string Field, string Reason)> ModeProblems(PipelineCompute compute, BuiltTransformationConfiguration built)
    {
        if (built.TableOutputs.Count > 0)
        {
            return StageComputeSetCheck.Problems(compute, built, _logger);
        }

        var problems = InputProblems(compute, built).Select(reason => ("Compute.Inputs", reason)).ToList();
        var stated = compute.Tables.Count > 0 ? "table bindings" : compute.Outputs.Count > 0 ? "output routes" : compute.Scalars.Count > 0 ? "single values" : compute.OnRowFailure is not null ? "onRowFailure" : null;
        if (stated is not null)
        {
            problems.Add(("Compute.Tables", ComputeStageLog.SetMemberInRecordMode(_logger, built.Name, stated).Message));
        }

        return problems;
    }

    private string? ShapeProblem(PipelineCompute compute, string? name)
    {
        if (name is not null && compute.TransformationId is not null)
        {
            return ComputeStageLog.NamesBoth(_logger).Message;
        }

        return name is null && compute.TransformationId is null ? ComputeStageLog.NamesNeither(_logger).Message : null;
    }

    // Every declared input has exactly one field, and every mapping names a declared input.
    private List<string> InputProblems(PipelineCompute compute, BuiltTransformationConfiguration built)
    {
        var reasons = new List<string>();
        if (compute.Inputs.Any(input => string.IsNullOrWhiteSpace(input.InputName) || string.IsNullOrWhiteSpace(input.SourceField)))
        {
            reasons.Add(ComputeStageLog.InputBlank(_logger).Message);
            return reasons;
        }

        foreach (var repeated in compute.Inputs.GroupBy(input => input.InputName, StringComparer.Ordinal).Where(group => group.Count() > 1))
        {
            reasons.Add(ComputeStageLog.InputMappedTwice(_logger, repeated.Key).Message);
        }

        foreach (var declared in built.Inputs.Where(declared => !compute.Inputs.Any(input => string.Equals(input.InputName, declared.Name, StringComparison.Ordinal))))
        {
            reasons.Add(ComputeStageLog.InputNotMapped(_logger, built.Name, declared.Name).Message);
        }

        foreach (var input in compute.Inputs.Where(input => !built.Inputs.Any(declared => string.Equals(declared.Name, input.InputName, StringComparison.Ordinal))))
        {
            reasons.Add(ComputeStageLog.InputNotDeclared(_logger, built.Name, input.InputName).Message);
        }

        return reasons;
    }

    private static PipelineProblem Problem(IReadOnlyList<string> stageNames, string stagePositions, string field, string reason) =>
        stageNames.Any(string.IsNullOrWhiteSpace)
            ? PipelineProblems.OfPipeline($"{stagePositions}.{field}", reason)
            : PipelineProblems.OfStage(stageNames, field, reason);
}
