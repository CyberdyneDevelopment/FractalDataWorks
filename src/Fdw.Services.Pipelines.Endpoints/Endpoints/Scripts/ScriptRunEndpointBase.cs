using System;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Results;
using Fdw.Services.Etl.Scripts;
using Fdw.Services.Pipelines.Clients.Abstractions;
using Fdw.Web.RestEndpoints.ErrorMapping;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// What the two endpoints that run a script share (FDW-1442): the script service the script runs through, the policy a caller holds
/// to run one, and the answer, which is the result mapped as every endpoint maps one.
/// </summary>
/// <typeparam name="TRequest">The request the endpoint takes.</typeparam>
public abstract class ScriptRunEndpointBase<TRequest> : Endpoint<TRequest, ScriptResultPayload>
    where TRequest : notnull
{
    /// <summary>Initializes a new instance of the <see cref="ScriptRunEndpointBase{TRequest}"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="scripts">Runs a script through the data gateway.</param>
    protected ScriptRunEndpointBase(ILogger? logger, IScriptService scripts)
    {
        RunLogger = logger ?? NullLogger.Instance;
        Scripts = scripts ?? throw new ArgumentNullException(nameof(scripts));
    }

    /// <summary>Gets the logger.</summary>
    protected ILogger RunLogger { get; }

    /// <summary>Gets the service that runs a script.</summary>
    protected IScriptService Scripts { get; }

    /// <summary>Gets the policy a caller holds to run a script.</summary>
    protected virtual string ExecutePolicy => "scripts:execute";

    /// <summary>Sets what the two endpoints have in common; each states its own route first.</summary>
    /// <param name="summary">The one-line summary.</param>
    /// <param name="description">What the endpoint does.</param>
    protected void ConfigureRun(string summary, string description)
    {
        DontThrowIfValidationFails();
#if DEVELOP
        AllowAnonymous();
#else
        Policies(ExecutePolicy);
#endif
        Summary(s =>
        {
            s.Summary = summary;
            s.Description = description;
        });
    }

    /// <summary>Answers the run: what the script answered, or the failure mapped to its status.</summary>
    /// <param name="result">What the run answered.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task that ends when the answer is sent.</returns>
    protected Task Answer(IGenericResult<ScriptResultPayload> result, CancellationToken ct)
    {
        if (result.IsSuccess && result.Value is { } payload)
        {
            return Send.OkAsync(payload, ct);
        }

        var (statusCode, errorResponse) = ResultHttpStatusMapper.Map(result, HttpContext);
        return Send.ResultAsync(TypedResults.Json(errorResponse, statusCode: statusCode));
    }
}
