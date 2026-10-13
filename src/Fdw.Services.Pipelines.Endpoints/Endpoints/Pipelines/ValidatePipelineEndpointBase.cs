using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Data.Transformations.Configuration;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Data;
using Fdw.Services.Notifications.Abstractions;
using Fdw.Services.Pipelines.Clients.Abstractions;
using Fdw.Services.Pipelines.Endpoints.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// Base endpoint that checks a pipeline draft exactly as creating it would, without saving anything, and answers
/// every problem it finds so the Pipeline Builder can show them against their stages before the person saves
/// (FDW-1095). Route: POST /pipelines/validate.
/// </summary>
/// <remarks>
/// Why the same checks and not a copy: request validation is the create request's own validator (FastEndpoints runs
/// it for this endpoint because the request type is the same, and <c>DontThrowIfValidationFails</c> hands the
/// failures to this handler instead of answering 400); the transform mapping and the name
/// check are the very calls <see cref="CreatePipelineEndpointBase{TConfig}"/> makes. A later check needs a draft
/// the earlier one accepted, so the phases run in that order and a draft with request-shape problems is not also
/// judged on its load definition. An empty problem list means create would not refuse the draft for any reason these
/// checks can see.
/// </remarks>
public abstract class ValidatePipelineEndpointBase : Endpoint<CreatePipelineRequest, ValidatePipelineResponse>
{
    private readonly ILogger _logger;
    private readonly PipelineServiceConfigurationProvider _provider;
    private readonly PipelineLoadDraft _loadDraft;
    private readonly StageChainLookup _chains;

    /// <summary>Initializes a new instance of the <see cref="ValidatePipelineEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="provider">Resolves existing pipelines, to find a name that is already taken.</param>
    /// <param name="dataSets">Resolves data sets as the caller.</param>
    /// <param name="connections">Resolves connections as the caller.</param>
    /// <param name="dataStoreConfigurations">Resolves the data stores an Execute load step's container is read from.</param>
    /// <param name="transformations">Resolves the named Transformations a field mapping's chain applies.</param>
    protected ValidatePipelineEndpointBase(
        ILogger<ValidatePipelineEndpointBase> logger,
        PipelineServiceConfigurationProvider provider,
        IDataSetConfigurationProvider dataSets,
        IConnectionConfigurationProvider connections,
        DataStoreConfigurationProvider dataStoreConfigurations,
        ITransformationConfigurationProvider transformations)
    {
        _logger = logger;
        _provider = provider;
        _loadDraft = new PipelineLoadDraft(dataSets, dataStoreConfigurations, connections, logger);
        _chains = new StageChainLookup(transformations, logger);
    }

    /// <summary>Gets the authorization policy name for this operation: whoever may create a pipeline may check a draft.</summary>
    protected virtual string WritePolicy => "pipelines:write";

    /// <summary>Configures the endpoint route, policies, and OpenAPI metadata.</summary>
    public override void Configure()
    {
        Post("/pipelines/validate");
        DontThrowIfValidationFails();
#if DEVELOP
        AllowAnonymous();
#else
        Policies(WritePolicy);
#endif
        Summary(s =>
        {
            s.Summary = "Check a pipeline draft without saving it";
            s.Description = "Takes the body of POST /pipelines and answers every problem creating it would be refused for, each placed on a stage and a field where it can be. An empty list means create would accept the draft.";
        });
    }

    /// <summary>Checks the draft and answers its problems.</summary>
    /// <param name="req">The draft, in the shape of a create request.</param>
    /// <param name="ct">The cancellation token.</param>
    public override async Task HandleAsync(CreatePipelineRequest req, CancellationToken ct)
    {
        PipelineEndpointLog.DraftCheckStarted(_logger, req.Name);

        var problems = new List<PipelineProblem>();
        foreach (var failure in ValidationFailures)
        {
            problems.Add(PipelineProblems.FromFailure(failure.PropertyName, failure.ErrorMessage, req));
        }

        if (problems.Count == 0)
        {
            await CheckDraft(req, problems, ct).ConfigureAwait(false);
        }

        PipelineEndpointLog.DraftCheckFinished(_logger, req.Name, problems.Count);
        await Send.OkAsync(new ValidatePipelineResponse { Problems = problems }, ct).ConfigureAwait(false);
    }

    private async Task CheckDraft(CreatePipelineRequest req, List<PipelineProblem> problems, CancellationToken ct)
    {
        if (await PipelineDraft.NameTaken(_provider, req.Name, ct).ConfigureAwait(false))
        {
            problems.Add(PipelineProblems.OfPipeline("Name", $"A pipeline named '{req.Name}' already exists"));
        }

        var chains = await _chains.Resolve(req.Name, req.Transforms, ct).ConfigureAwait(false);
        problems.AddRange(chains.Problems);
        if (chains.Problems.Count == 0)
        {
            var transforms = PipelineTransformConfigurationMapper.Map(req.PipelineType, req.Transforms, _logger, chains.Ids);
            if (transforms.IsFailure)
            {
                problems.Add(PipelineProblems.OfStageRefusal(transforms, "mapping the stages"));
            }
            else if (await PipelineNotifyCheck.Check(transforms.Value!, TryResolve<IRuleNotifier>(), _logger, ct).ConfigureAwait(false) is { IsFailure: true } refused)
            {
                problems.Add(PipelineProblems.OfStageRefusal(refused, "checking the notification rules"));
            }
        }

        problems.AddRange(await _loadDraft.Problems(req, req.DestinationDataSet, PipelineDraft.StageFilterExpressions(req.Transforms), ct).ConfigureAwait(false));

        if (PipelineTruncateConsistency.Check(req.TruncateBeforeLoad, req.LoadMode, _logger) is { } disagreement)
        {
            problems.Add(PipelineProblems.OfPipeline("TruncateBeforeLoad", disagreement.Message));
        }
    }
}
