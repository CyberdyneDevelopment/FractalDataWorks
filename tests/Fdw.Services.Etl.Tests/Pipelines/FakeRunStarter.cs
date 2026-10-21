using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Execution;

namespace Fdw.Services.Etl.Tests.Pipelines;

/// <summary>
/// A run starter fake for the StartPipeline tests (FDW-1381): it records each start and check in order, and can be told that a
/// pipeline does not exist or that a start fails.
/// </summary>
internal sealed class FakeRunStarter : IPipelineRunStarter
{
    /// <summary>Gets the starts asked for, in order.</summary>
    public List<PipelineRunStart> Started { get; } = [];

    /// <summary>Gets the pipelines checked, with the parameter names the stage bound, in order.</summary>
    public List<(string Pipeline, IReadOnlyList<string> Names)> Checked { get; } = [];

    /// <summary>Gets the pipelines that do not exist.</summary>
    public HashSet<string> MissingPipelines { get; } = new(StringComparer.Ordinal);

    /// <summary>Gets or sets what <see cref="Await"/> answers: whether a started run succeeded.</summary>
    public bool AwaitResult { get; set; } = true;

    /// <summary>Gets the pipelines whose start fails.</summary>
    public HashSet<string> FailingPipelines { get; } = new(StringComparer.Ordinal);

    /// <inheritdoc />
    public Task<IGenericResult<Guid>> Start(PipelineRunStart start, CancellationToken cancellationToken = default)
    {
        Started.Add(start);
        return Task.FromResult(FailingPipelines.Contains(start.PipelineName!)
            ? GenericResult<Guid>.Failure(new GenericMessage($"pipeline '{start.PipelineName}' could not be started"))
            : GenericResult<Guid>.Success(Guid.NewGuid()));
    }

    /// <inheritdoc />
    public Task<IGenericResult> Check(string pipelineName, IReadOnlyList<string> parameterNames, CancellationToken cancellationToken = default)
    {
        Checked.Add((pipelineName, parameterNames));
        return Task.FromResult(MissingPipelines.Contains(pipelineName)
            ? GenericResult.Failure(new GenericMessage($"pipeline '{pipelineName}' was not found"))
            : GenericResult.Success());
    }

    /// <inheritdoc />
    public Task<bool> Await(Guid executionId, CancellationToken cancellationToken = default) => Task.FromResult(AwaitResult);
}
