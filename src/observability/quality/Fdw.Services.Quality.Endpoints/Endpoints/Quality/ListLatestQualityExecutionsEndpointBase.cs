using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Quality;
using Fdw.Web.RestEndpoints.ErrorMapping;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Quality.Endpoints;

/// <summary>Endpoint that answers the latest recorded execution of each rule of a data set (FDW-1363).</summary>
/// <remarks>
/// One row per rule that has been executed. A rule that has never been executed has no row: it is "never checked", and it
/// is never given a verdict to stand in for one nobody computed.
/// </remarks>
public abstract class ListLatestQualityExecutionsEndpointBase : Endpoint<DataSetQueryRequest, List<QualityRuleExecutionResponse>>
{
    private readonly IQualityRuleConfigurationProvider _provider;
    private readonly IDataGatewayProvider _dataGateways;

    // Why resolved here rather than injected: the gateway is scoped and this is not (see ExecuteQualityCheckEndpointBase).
    private IDataGateway Gateway => _dataGateways.ByName("Main");

    /// <summary>Initializes a new instance of the <see cref="ListLatestQualityExecutionsEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="provider">The quality rule provider.</param>
    /// <param name="dataGateways">The data gateways.</param>
    protected ListLatestQualityExecutionsEndpointBase(ILogger logger, IQualityRuleConfigurationProvider provider, IDataGatewayProvider dataGateways)
    {
        Logger = logger;
        _provider = provider;
        _dataGateways = dataGateways;
    }

    /// <summary>Gets the authorization policy required for read operations.</summary>
    protected virtual string ReadPolicy => "datasets:read";

    /// <summary>Gets the logger instance for this endpoint.</summary>
    protected new ILogger Logger { get; }

    /// <summary>Configures the endpoint route, policies, and OpenAPI metadata.</summary>
    public override void Configure()
    {
        Get("/quality/datasets/{DataSetName}/executions/latest");
#if DEVELOP
        AllowAnonymous();
#else
        Policies(ReadPolicy);
#endif
        Summary(s =>
        {
            s.Summary = "The latest recorded execution of each quality rule of a data set";
            s.Description = "One row per rule that has been executed. A rule that has never been executed has no row.";
        });
    }

    /// <summary>Reads the data set's rules and the newest recorded execution of each.</summary>
    public override async Task HandleAsync(DataSetQueryRequest req, CancellationToken ct)
    {
        var named = QualityRuleTarget.RequireDataSet("The request", req.DataSetName, Logger);
        if (named.IsFailure)
        {
            await Send.ResultAsync(TypedResults.Json(new { Error = "Request names no data set", Details = named.CurrentMessage }, statusCode: 400)).ConfigureAwait(false);
            return;
        }

        var tenant = QualityRuleExecutions.CallerTenant(User, Logger);
        if (tenant.IsFailure)
        {
            await Send.ResultAsync(TypedResults.Json(new { Error = "No active tenant", Details = tenant.CurrentMessage }, statusCode: 400)).ConfigureAwait(false);
            return;
        }

        var rulesResult = await _provider.Get(ct).ConfigureAwait(false);
        if (!rulesResult.IsSuccess)
        {
            await Send.ResultAsync(TypedResults.Json(new { Error = "Failed to fetch quality rules", Details = rulesResult.CurrentMessage }, statusCode: 500)).ConfigureAwait(false);
            return;
        }

        // Why one small read per rule and not the newest N rows of the data set reduced in memory: a rule last executed
        // before those N rows would be missing from the answer and read as "never checked", which it is not.
        var latest = new List<QualityRuleExecutionResponse>();
        foreach (var rule in (rulesResult.Value ?? []).Where(r => string.Equals(r.DataSetName, req.DataSetName, StringComparison.OrdinalIgnoreCase)))
        {
            var read = await QualityRuleExecutions.ForRule(
                Gateway, QualityServiceTypes.OperationalConnection, tenant.Value, rule.Id, 1, Logger, ct).ConfigureAwait(false);
            if (read.IsFailure || read.Value is not { } rows)
            {
                var (statusCode, problem) = ResultHttpStatusMapper.Map(read, HttpContext);
                await Send.ResultAsync(TypedResults.Json(problem, statusCode: statusCode)).ConfigureAwait(false);
                return;
            }

            latest.AddRange(rows.Select(QualityRuleExecutions.ToResponse));
        }

        await Send.OkAsync(latest, ct).ConfigureAwait(false);
    }
}
