using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Data.Transformations.Configuration;
using Fdw.Services.Etl.Graphs;
using Fdw.Services.Pipelines.Clients.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// Checks a graph draft without saving it (FDW-1396) and answers every problem, each with the path to the part it concerns.
/// </summary>
/// <remarks>
/// It judges what saving would judge: each component's settings against its kind and the shape of the whole graph. It does not
/// judge the name, so one route serves a new graph and an edit of a saved one, and it does not reach any data, so a data set
/// that does not exist is the run's to refuse. A body it cannot read at all (a setting of the wrong type) comes back as one
/// problem with no path.
/// </remarks>
public abstract class ValidateGraphEndpointBase : Endpoint<GraphPayload, GraphValidationPayload>
{
    private readonly ILogger _logger;
    private readonly StageChainLookup _chains;

    /// <summary>Initializes a new instance of the <see cref="ValidateGraphEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="transformations">Resolves the named transformations a field mapping's chain applies.</param>
    protected ValidateGraphEndpointBase(ILogger<ValidateGraphEndpointBase> logger, ITransformationConfigurationProvider transformations)
    {
        _logger = logger;
        _chains = new StageChainLookup(transformations, logger);
    }

    /// <summary>Gets the authorization policy the check needs.</summary>
    protected virtual string WritePolicy => "pipelines:write";

    /// <inheritdoc />
    public override void Configure()
    {
        Post("/pipelines/graph/validate");
        DontThrowIfValidationFails();
#if DEVELOP
        AllowAnonymous();
#else
        Policies(WritePolicy);
#endif
        Summary(s =>
        {
            s.Summary = "Check a graph pipeline draft without saving it";
            s.Description = "Takes the body of POST /pipelines/graph and answers every problem saving it would be refused for, each with the path to the part it concerns. An empty list means saving would accept the draft.";
        });
    }

    /// <inheritdoc />
    public override async Task HandleAsync(GraphPayload req, CancellationToken ct)
    {
        var unread = new System.Collections.Generic.List<GraphProblem>();
        var graph = GraphPayloadReader.ReadDraft(req, Guid.Empty, unread, await GraphChainLookup.Resolve(_chains, req, ct).ConfigureAwait(false));
        var answer = graph.IsFailure || graph.Value is null
            ? new GraphValidationPayload
            {
                IsValid = false,
                Problems = [new GraphProblemPayload { Path = [], Reason = PipelineProblems.ReasonOf(graph.CurrentMessage, "reading the graph") }],
            }
            : Answer([.. unread, .. GraphShapeValidator.Problems(graph.Value, _logger)]);
        await Send.OkAsync(answer, ct).ConfigureAwait(false);
    }

    private static GraphValidationPayload Answer(System.Collections.Generic.IReadOnlyList<GraphProblem> problems) => new()
    {
        IsValid = problems.Count == 0,
        Problems = [.. problems.Select(problem => new GraphProblemPayload { Path = [.. problem.Path], Reason = problem.Reason })],
    };
}
