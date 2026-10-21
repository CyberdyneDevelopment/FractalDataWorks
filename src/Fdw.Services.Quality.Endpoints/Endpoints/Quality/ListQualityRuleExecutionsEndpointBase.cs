using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Quality;
using Fdw.Web.RestEndpoints.ErrorMapping;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Quality.Endpoints;

/// <summary>Endpoint that lists the recorded executions of one quality rule, newest first (FDW-1363).</summary>
/// <remarks>
/// Answers what the rule found each time it ran: the rows that met it, the rows that violated it, and why it could not run
/// when it could not. A rule that has never been executed answers an empty list, never a verdict.
/// </remarks>
public abstract class ListQualityRuleExecutionsEndpointBase : Endpoint<QualityRuleExecutionsRequest, List<QualityRuleExecutionResponse>>
{
    private readonly IDataGatewayProvider _dataGateways;

    // Why resolved here rather than injected: the gateway is scoped and this is not (see ExecuteQualityCheckEndpointBase).
    private IDataGateway Gateway => _dataGateways.ByName("Main");

    /// <summary>Initializes a new instance of the <see cref="ListQualityRuleExecutionsEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="dataGateways">The data gateways.</param>
    protected ListQualityRuleExecutionsEndpointBase(ILogger logger, IDataGatewayProvider dataGateways)
    {
        Logger = logger;
        _dataGateways = dataGateways;
    }

    /// <summary>Gets the authorization policy required for read operations.</summary>
    protected virtual string ReadPolicy => "datasets:read";

    /// <summary>Gets the logger instance for this endpoint.</summary>
    protected new ILogger Logger { get; }

    /// <summary>Configures the endpoint route, policies, and OpenAPI metadata.</summary>
    public override void Configure()
    {
        Get("/quality/rules/{Id}/executions");
#if DEVELOP
        AllowAnonymous();
#else
        Policies(ReadPolicy);
#endif
        Summary(s =>
        {
            s.Summary = "List the recorded executions of a quality rule";
            s.Description = "The newest executions first. Take is required, from 1 to 500. Each row says how many rows met the rule, "
                + "how many violated it, and how many were not judged (with the reason when the rule could not run).";
        });
    }

    /// <summary>Reads the newest recorded executions of the rule under the caller's active tenant.</summary>
    public override async Task HandleAsync(QualityRuleExecutionsRequest req, CancellationToken ct)
    {
        if (req.Take is < 1 or > QualityRuleExecutions.MaxTake)
        {
            var refusal = GenericResult.Failure(QualityCheckEndpointLog.TakeOutOfRange(Logger, QualityRuleExecutions.MaxTake, req.Take));
            await Send.ResultAsync(TypedResults.Json(new { Error = "Take is out of range", Details = refusal.CurrentMessage }, statusCode: 400)).ConfigureAwait(false);
            return;
        }

        var tenant = QualityRuleExecutions.CallerTenant(User, Logger);
        if (tenant.IsFailure)
        {
            await Send.ResultAsync(TypedResults.Json(new { Error = "No active tenant", Details = tenant.CurrentMessage }, statusCode: 400)).ConfigureAwait(false);
            return;
        }

        var read = await QualityRuleExecutions.ForRule(
            Gateway, QualityServiceTypes.OperationalConnection, tenant.Value, req.Id, req.Take, Logger, ct).ConfigureAwait(false);
        if (read.IsFailure || read.Value is not { } rows)
        {
            var (statusCode, problem) = ResultHttpStatusMapper.Map(read, HttpContext);
            await Send.ResultAsync(TypedResults.Json(problem, statusCode: statusCode)).ConfigureAwait(false);
            return;
        }

        await Send.OkAsync(rows.Select(QualityRuleExecutions.ToResponse).ToList(), ct).ConfigureAwait(false);
    }
}
