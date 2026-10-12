using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Data.Transformations.Configuration;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Data;
using Fdw.Services.Notifications.Abstractions;
using Fdw.Services.Pipelines.Abstractions;
using Fdw.Services.Pipelines.Clients.Abstractions;
using Fdw.Services.Pipelines.Endpoints.Logging;
using Fdw.Web.RestEndpoints.ErrorMapping;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// Base endpoint that checks an edit of a saved pipeline exactly as updating it would, without saving anything, and
/// answers every problem it finds so the Pipeline Builder can show them against their stages before the person saves
/// (FDW-1105). Route: POST /pipelines/{name}/validate.
/// </summary>
/// <remarks>
/// Why a route of its own and not a mode of <c>POST /pipelines/validate</c>: that endpoint takes the body of a create, which
/// names the pipeline's kind and both sides, and must refuse a name that is taken; an edit is the body of an update, whose
/// every property is optional and whose name is in the route, and the pipeline it is about must exist. The two
/// questions have two bodies, as create and update do. The body here is exactly <see cref="UpdatePipelineRequest"/>, so
/// FastEndpoints runs the update's own request validator for this endpoint and, with <c>DontThrowIfValidationFails</c>,
/// hands the failures to this handler instead of answering 400. The rest of the checks are the update's, in
/// <see cref="SavedPipelineCheck"/>. A body that states nothing to change is NOT a problem here: the update refuses it because
/// saving nothing is a no-op, but checking a saved pipeline as it stands is what a person does first and the Builder sends
/// exactly that body when nothing was edited. The validator marks that one failure with
/// <see cref="UpdatePipelineChanges.NothingStatedCode"/> and this handler leaves it out.
/// </remarks>
public abstract class ValidateExistingPipelineEndpointBase : Endpoint<UpdatePipelineRequest, ValidatePipelineResponse>
{
    private readonly PipelineServiceConfigurationProvider _provider;
    private readonly SavedPipelineCheck _check;

    /// <summary>Initializes a new instance of the <see cref="ValidateExistingPipelineEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="provider">Reads the saved pipeline the edit is about.</param>
    /// <param name="dataSets">Resolves the pipeline's data sets as the caller.</param>
    /// <param name="connections">Resolves the pipeline's connections as the caller.</param>
    /// <param name="dataStoreConfigurations">Resolves the data stores an Execute load step's container is read from.</param>
    /// <param name="transformations">Resolves the named Transformations a field mapping's chain applies.</param>
    protected ValidateExistingPipelineEndpointBase(
        ILogger<ValidateExistingPipelineEndpointBase> logger,
        PipelineServiceConfigurationProvider provider,
        IDataSetConfigurationProvider dataSets,
        IConnectionConfigurationProvider connections,
        DataStoreConfigurationProvider dataStoreConfigurations,
        ITransformationConfigurationProvider transformations)
    {
        EndpointLogger = logger;
        _provider = provider;
        _check = new SavedPipelineCheck(
            new PipelineLoadDraft(dataSets, dataStoreConfigurations, connections, logger),
            new StageChainLookup(transformations, logger),
            logger);
    }

    /// <summary>Gets the logger.</summary>
    protected ILogger EndpointLogger { get; }

    /// <summary>Gets the authorization policy name for this operation: whoever may update a pipeline may check an edit of it.</summary>
    protected virtual string WritePolicy => "pipelines:write";

    /// <summary>Configures the endpoint route, policies, and OpenAPI metadata.</summary>
    public override void Configure()
    {
        Post("/pipelines/{Name}/validate");
        DontThrowIfValidationFails();
#if DEVELOP
        AllowAnonymous();
#else
        Policies(WritePolicy);
#endif
        Summary(s =>
        {
            s.Summary = "Check an edit of a saved pipeline without saving it";
            s.Description = "Takes the body of PATCH /pipelines/{name} and answers every problem updating the pipeline with it would be refused for, each placed on a stage and a field where it can be. An empty list means the update would accept the edit. A body that states nothing to change checks the saved pipeline as it stands. Answers 404 when no pipeline has the name.";
        });
    }

    /// <summary>
    /// Says whether the edit can be applied to this kind of saved pipeline at all. The concrete endpoint answers with the
    /// same limit its update endpoint states.
    /// </summary>
    /// <param name="request">The update body.</param>
    /// <param name="original">The saved pipeline.</param>
    /// <returns>Success, or the refusal the update would give.</returns>
    protected abstract IGenericResult CheckApplicable(UpdatePipelineRequest request, IPipelineImplementationConfiguration original);

    /// <summary>Checks the edit and answers its problems.</summary>
    /// <param name="req">The edit, in the shape of an update request, with the pipeline's name from the route.</param>
    /// <param name="ct">The cancellation token.</param>
    public override async Task HandleAsync(UpdatePipelineRequest req, CancellationToken ct)
    {
        PipelineEndpointLog.EditCheckStarted(EndpointLogger, req.Name);

        var saved = await _provider.Get(req.Name, ct).ConfigureAwait(false);
        if (saved.IsFailure)
        {
            var (status, problem) = ResultHttpStatusMapper.Map(saved, HttpContext);
            await Send.ResultAsync(TypedResults.Json(problem, statusCode: status)).ConfigureAwait(false);
            return;
        }

        if (saved.Value is null)
        {
            await SendNotFound(req.Name).ConfigureAwait(false);
            return;
        }

        var problems = new List<PipelineProblem>();
        foreach (var failure in ValidationFailures.Where(failure => !string.Equals(failure.ErrorCode, UpdatePipelineChanges.NothingStatedCode, StringComparison.Ordinal)))
        {
            problems.Add(PipelineProblems.FromFailure(failure.PropertyName, failure.ErrorMessage, req.Transforms));
        }

        if (problems.Count == 0)
        {
            problems.AddRange(await CheckEdit(req, saved.Value, ct).ConfigureAwait(false));
        }

        PipelineEndpointLog.EditCheckFinished(EndpointLogger, req.Name, problems.Count);
        await Send.OkAsync(new ValidatePipelineResponse { Problems = problems }, ct).ConfigureAwait(false);
    }

    private async Task<IReadOnlyList<PipelineProblem>> CheckEdit(UpdatePipelineRequest req, IPipelineImplementationConfiguration saved, CancellationToken ct)
    {
        var applicable = CheckApplicable(req, saved);
        if (applicable.IsFailure)
        {
            return [PipelineProblems.OfPipeline("PipelineType", PipelineProblems.ReasonOf(applicable.CurrentMessage, "checking the pipeline's kind"))];
        }

        return await _check.Problems(req, saved, TryResolve<IRuleNotifier>(), ct).ConfigureAwait(false);
    }

    private async Task SendNotFound(string name)
    {
        var message = PipelineEndpointLog.PipelineNotFound(EndpointLogger, name);
        await Send.ResultAsync(TypedResults.Json(
            new Microsoft.AspNetCore.Mvc.ProblemDetails
            {
                Status = 404,
                Title = "Not found",
                Detail = message.Message,
                Instance = HttpContext.Request.Path.Value,
            },
            statusCode: 404)).ConfigureAwait(false);
    }
}
