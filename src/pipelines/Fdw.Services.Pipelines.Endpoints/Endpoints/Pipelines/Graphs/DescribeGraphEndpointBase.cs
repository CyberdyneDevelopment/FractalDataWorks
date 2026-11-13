using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Data.Transformations.Configuration;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Data;
using Fdw.Services.Etl.Graphs;
using Fdw.Services.Etl.Graphs.Describing;
using Fdw.Services.Pipelines.Clients.Abstractions;
using Microsoft.Extensions.Logging;
using Fdw.Web.RestEndpoints.ErrorMapping;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// Describes a graph draft (FDW-1397): the fields a row has at every input and output node, and the problems the draft has.
/// </summary>
/// <remarks>
/// It reads the fields of each data set a source names as the caller, so a data set the caller cannot see is refused as it is
/// anywhere else. Nothing is saved. A node whose fields cannot be known before a run answers null fields, not an empty list.
/// </remarks>
public abstract class DescribeGraphEndpointBase : Endpoint<GraphPayload, GraphDescriptionPayload>
{
    private readonly ILogger _logger;
    private readonly IDataSetConfigurationProvider _dataSets;
    private readonly StageChainLookup _chains;

    /// <summary>Initializes a new instance of the <see cref="DescribeGraphEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="dataSets">Reads the fields of a data set.</param>
    /// <param name="transformations">Resolves the named transformations a field mapping's chain applies.</param>
    protected DescribeGraphEndpointBase(
        ILogger<DescribeGraphEndpointBase> logger, IDataSetConfigurationProvider dataSets, ITransformationConfigurationProvider transformations)
    {
        _logger = logger;
        _dataSets = dataSets;
        _chains = new StageChainLookup(transformations, logger);
    }

    /// <summary>Gets the authorization policy the description needs.</summary>
    protected virtual string ReadPolicy => "pipelines:write";

    /// <inheritdoc />
    public override void Configure()
    {
        Post("/pipelines/graph/describe");
        DontThrowIfValidationFails();
#if DEVELOP
        AllowAnonymous();
#else
        Policies(ReadPolicy);
#endif
        Summary(s =>
        {
            s.Summary = "Describe a graph pipeline draft";
            s.Description = "Takes the body of POST /pipelines/graph and answers the fields a row has at every input and output node, and every problem saving it would be refused for. Nothing is saved.";
        });
    }

    /// <inheritdoc />
    public override async Task HandleAsync(GraphPayload req, CancellationToken ct)
    {
        var unread = new List<GraphProblem>();
        var graph = GraphPayloadReader.ReadDraft(req, Guid.Empty, unread, await GraphChainLookup.Resolve(_chains, req, ct).ConfigureAwait(false));
        if (graph.IsFailure || graph.Value is null)
        {
            await Send.OkAsync(
                new GraphDescriptionPayload { Problems = [new GraphProblemPayload { Path = [], Reason = PipelineProblems.ReasonOf(graph.CurrentMessage, "reading the graph") }] },
                ct).ConfigureAwait(false);
            return;
        }

        // Why the data store provider is resolved here: the save routes of this domain reach it the same way, and a describe that holds one more constructor parameter would change every host that derives it.
        var sources = new GraphDataSetSources(_dataSets, Resolve<IDataStoreConfigurationProvider>());
        var context = new DescribeContext(
            (name, token) => GraphDataSetFields.Read(_dataSets, name, token),
            (request, token) => GraphContainerFields.Read(sources, request, _logger, token));
        var described = await GraphDescriber.Describe(graph.Value, context, _logger, ct).ConfigureAwait(false);
        if (described.IsFailure)
        {
            await Send.ResultAsync(ResultHttpStatusMapper.Answer(described, HttpContext)).ConfigureAwait(false);
            return;
        }

        if (described.Value is null)
        {
            await Send.ResultAsync(ResultHttpStatusMapper.Answer(GenericResult.Failure(PipelinesAnswerLog.DescribeGraphDescribeReturnedNothing(_logger)), HttpContext)).ConfigureAwait(false);
            return;
        }

        await Send.OkAsync(
            new GraphDescriptionPayload
            {
                Nodes = [.. described.Value.Select(GraphNodePayloads.Of)],
                Scope = [.. GraphScopeEntries.Of(graph.Value)],
                Problems =
                [
                    .. unread.Concat(GraphShapeValidator.Problems(graph.Value, _logger)).Concat(context.Problems)
                        .Select(problem => new GraphProblemPayload { Path = [.. problem.Path], Reason = problem.Reason }),
                ],
            },
            ct).ConfigureAwait(false);
    }
}
