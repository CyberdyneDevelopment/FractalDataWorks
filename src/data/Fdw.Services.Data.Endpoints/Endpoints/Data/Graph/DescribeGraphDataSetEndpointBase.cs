using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Data.Transformations.Configuration;
using Fdw.Results;
using Fdw.Services.Etl.Graphs;
using Fdw.Services.Etl.Graphs.Describing;
using Fdw.Services.Pipelines.Clients.Abstractions;
using Fdw.Services.Pipelines.Endpoints;
using Fdw.Web.RestEndpoints.ErrorMapping;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Endpoint base to describe a Graph data set draft (FDW-5504): <c>POST /datasets/graph/describe</c>.
/// </summary>
/// <remarks>
/// <para>
/// It takes the body of <c>POST /datasets/graph</c> and answers the fields a row has at every input and output node of its graph, by the mechanism
/// <c>POST /pipelines/graph/describe</c> uses: the same reader of the graph, the same engine description and the same node answer. Nothing is saved.
/// </para>
/// <para>
/// The draft is judged by <see cref="GraphDataSetValidator.Draft"/>, as the validate and preview routes judge it, so the stated fields-out output counts as consumed and a
/// graph with no destination describes. A draft with a problem answers its problems, each with its code and path, and the nodes the engine could describe.
/// </para>
/// </remarks>
public abstract class DescribeGraphDataSetEndpointBase : Endpoint<CreateGraphDataSetRequest, GraphDataSetDescriptionPayload>
{
    private readonly ILogger<DescribeGraphDataSetEndpointBase> _logger;
    private readonly IDataSetConfigurationProvider _dataSets;
    private readonly GraphDataSetSources _sources;
    private readonly StageChainLookup _chains;

    /// <summary>Initializes a new instance of the <see cref="DescribeGraphDataSetEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="dataSets">Reads the data sets the draft reads, for their fields.</param>
    /// <param name="dataStores">Reads the data stores the draft reads or intends to read.</param>
    /// <param name="transformations">Reads the named transformations a field mapping applies.</param>
    protected DescribeGraphDataSetEndpointBase(
        ILogger<DescribeGraphDataSetEndpointBase> logger,
        IDataSetConfigurationProvider dataSets,
        IDataStoreConfigurationProvider dataStores,
        ITransformationConfigurationProvider transformations)
    {
        _logger = logger;
        _dataSets = dataSets;
        _sources = new GraphDataSetSources(dataSets, dataStores);
        _chains = new StageChainLookup(transformations, logger);
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Post("/datasets/graph/describe");
        DontThrowIfValidationFails();
#if DEVELOP
        AllowAnonymous();
#else
        Policies("datasets:write");
#endif
        Summary(s =>
        {
            s.Summary = "Describe a Graph data set draft";
            s.Description = "Takes the body of POST /datasets/graph and answers the fields a row has at every input and output node, as POST /pipelines/graph/describe does, and every problem of the draft with its code and path, as POST /datasets/graph/validate does. Nothing is saved.";
        });
        Tags("DataSets");
    }

    /// <inheritdoc />
    public override async Task HandleAsync(CreateGraphDataSetRequest req, CancellationToken ct)
    {
        // The graph is read as POST /pipelines/graph/describe reads it: from the body's name, components and links.
        var graphPayload = new GraphPayload { Name = req.Name, Components = req.Components, Links = req.Links };
        var checkedDraft = await GraphDataSetValidator.Draft(req, _sources, _logger, ct).ConfigureAwait(false);
        if (checkedDraft.IsFailure || checkedDraft.Value is null)
        {
            await SendFailure(checkedDraft).ConfigureAwait(false);
            return;
        }

        var problems = new List<GraphDataSetProblemPayload>(checkedDraft.Value.Payload.Problems);
        var unread = new List<GraphProblem>();
        var graph = GraphPayloadReader.ReadDraft(graphPayload, Guid.Empty, unread, await GraphChainLookup.Resolve(_chains, graphPayload, ct).ConfigureAwait(false));
        if (graph.IsFailure || graph.Value is null)
        {
            // The check already says the graph could not be read, with no path; there is nothing to describe.
            await Send.OkAsync(new GraphDataSetDescriptionPayload { IsValid = false, Problems = problems }, ct).ConfigureAwait(false);
            return;
        }

        var context = new DescribeContext(
            (name, token) => GraphDataSetFields.Read(_dataSets, name, token),
            (request, token) => GraphContainerFields.Read(_sources, request, _logger, token));
        var described = await GraphDescriber.Describe(graph.Value, context, _logger, ct).ConfigureAwait(false);
        if (described.IsFailure || described.Value is null)
        {
            await SendFailure(described).ConfigureAwait(false);
            return;
        }

        problems.AddRange(context.Problems.Select(problem => Payload(GraphDataSetSoundness.Invalid(req.Name, problem, _logger))));
        await Send.OkAsync(
            new GraphDataSetDescriptionPayload
            {
                IsValid = problems.Count == 0,
                Nodes = [.. described.Value.Select(GraphNodePayloads.Of)],
                Scope = [.. GraphScopeEntries.Of(graph.Value)],
                Problems = problems,
            },
            ct).ConfigureAwait(false);
    }

    private static GraphDataSetProblemPayload Payload(GraphDataSetProblem problem) =>
        new() { Code = problem.Code, Message = problem.Message, Path = [.. problem.Path] };

    private async Task SendFailure(IGenericResult failure)
    {
        var (statusCode, errorResponse) = ResultHttpStatusMapper.Map(failure, HttpContext);
        await Send.ResultAsync(TypedResults.Json(errorResponse, statusCode: statusCode)).ConfigureAwait(false);
    }
}
