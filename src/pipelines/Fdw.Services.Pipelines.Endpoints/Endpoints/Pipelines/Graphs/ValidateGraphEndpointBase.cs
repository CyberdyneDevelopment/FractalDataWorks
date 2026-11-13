using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Data.Transformations.Configuration;
using Fdw.Services.Etl.Graphs;
using Fdw.Services.Pipelines.Clients.Abstractions;
using Microsoft.Extensions.Logging;
using Fdw.Services.Data;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// Checks a graph draft without saving it (FDW-1396) and answers every problem, each with the path to the part it concerns.
/// </summary>
/// <remarks>
/// It judges what saving would judge: each component's settings against its kind and the shape of the whole graph. It does not
/// judge the name, so one route serves a new graph and an edit of a saved one. It reads the fields of a destination's data set only to
/// judge the columns a Guid-minting value source fills (FDW-3400); a data set that does not exist is the run's to refuse. A body it cannot read at all (a setting of the wrong type) comes back as one
/// problem with no path.
/// </remarks>
public abstract class ValidateGraphEndpointBase : Endpoint<GraphPayload, GraphValidationPayload>
{
    private readonly ILogger _logger;
    private readonly StageChainLookup _chains;
    private readonly IDataSetConfigurationProvider _dataSets;

    /// <summary>Initializes a new instance of the <see cref="ValidateGraphEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="transformations">Resolves the named transformations a field mapping's chain applies.</param>
    /// <param name="dataSets">The data sets, read for the type of the columns a destination's value sources fill.</param>
    protected ValidateGraphEndpointBase(
        ILogger<ValidateGraphEndpointBase> logger, ITransformationConfigurationProvider transformations, IDataSetConfigurationProvider dataSets)
    {
        _dataSets = dataSets;
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
            s.Description = "Takes the body of POST /pipelines/graph and answers every problem saving it would be refused for, each with the path to the part it concerns. An empty list means saving would accept the draft. warnings lists what the check cannot judge (a SQL step on a data store whose scripts this host cannot read, names outside the data store, a data store not yet discovered); they do not stop a save.";
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
            : Answer(unread, await GraphSave.Judge(graph.Value, _dataSets, Resolve<IDataStoreConfigurationProvider>(), _logger, ct).ConfigureAwait(false));
        await Send.OkAsync(answer, ct).ConfigureAwait(false);
    }

    // The draft's unread parts are problems as the judged ones are; a warning (FDW-5457) is information and leaves the draft valid.
    private static GraphValidationPayload Answer(System.Collections.Generic.List<GraphProblem> unread, GraphFindings findings) => new()
    {
        IsValid = unread.Count == 0 && findings.Problems.Count == 0,
        Problems = [.. unread.Concat(findings.Problems).Select(Payload)],
        Warnings = [.. findings.Warnings.Select(Payload)],
    };

    private static GraphProblemPayload Payload(GraphProblem problem) => new() { Path = [.. problem.Path], Reason = problem.Reason };
}
