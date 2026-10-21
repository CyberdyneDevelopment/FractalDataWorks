using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Commands.Data;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Quality;
using Fdw.Services.Quality.Configuration;
using Fdw.Web.RestEndpoints.ErrorMapping;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Quality.Endpoints;


/// <summary>Endpoint that executes all enabled quality checks for a specified DataSet.</summary>
public abstract class ExecuteAllQualityChecksEndpointBase : Endpoint<DataSetQueryRequest, List<QualityCheckResultResponse>>
{
    /// <summary>Initializes a new instance of the <see cref="ExecuteAllQualityChecksEndpointBase"/> class.</summary>
        private readonly IQualityRuleConfigurationProvider _provider;
    private readonly IDataGatewayProvider _dataGateways;

    // Why resolved here rather than injected: the gateway is scoped and this is not, so holding one
    // would be a captive dependency. The provider is asked when a call is actually being made.
    private IDataGateway Gateway => _dataGateways.ByName("Main");

    /// <summary>Initializes a new instance of the <see cref="ExecuteAllQualityChecksEndpointBase"/> class.</summary>
    protected ExecuteAllQualityChecksEndpointBase(ILogger logger, IQualityRuleConfigurationProvider provider, IDataGatewayProvider dataGateways)
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
        Post("/quality/datasets/{DataSetName}/execute");
#if DEVELOP
        AllowAnonymous();
#else
        Policies(ReadPolicy);
#endif
        Summary(s => s.Summary = "Execute all quality checks for a DataSet");
    }

    /// <summary>Fetches all enabled quality rules for the DataSet, queries the data, and executes each rule.</summary>
    public override async Task HandleAsync(DataSetQueryRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.DataSetName))
        {
            await Send.ResultAsync(TypedResults.Json(new { Error = "DataSetName is required" }, statusCode: 400)).ConfigureAwait(false);
            return;
        }

        // Why the tenant is read before the rows: see ExecuteQualityCheckEndpointBase.
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

        var rules = rulesResult.Value?
            .Where(r => string.Equals(r.DataSetName, req.DataSetName, StringComparison.OrdinalIgnoreCase)
                        && r.IsEnabled)
            .ToList() ?? [];

        if (rules.Count == 0)
        {
            await Send.OkAsync(new List<QualityCheckResultResponse>(), ct).ConfigureAwait(false);
            return;
        }

        var named = QualityRuleTarget.RequireDataSet("The request", req.DataSetName, Logger);
        if (named.IsFailure)
        {
            await Send.ResultAsync(TypedResults.Json(new { Error = "Request names no data set", Details = named.CurrentMessage }, statusCode: 400)).ConfigureAwait(false);
            return;
        }

        // Why a failed read is the answer and not one "Failed" entry per rule (FDW-1162): no rule ran. The read's own code
        // decides the status, and a refusal's reason reaches the caller through the mapper, written once.
        var rows = await QualityRuleTarget.ReadRows(Gateway, req.DataSetName, Logger, ct).ConfigureAwait(false);
        if (rows.IsFailure || rows.Value is not { } data)
        {
            var (statusCode, problem) = ResultHttpStatusMapper.Map(rows, HttpContext);
            await Send.ResultAsync(TypedResults.Json(problem, statusCode: statusCode)).ConfigureAwait(false);
            return;
        }

        var totalCount = data.Count;

        var results = new List<QualityCheckResultResponse>();

        foreach (var rule in rules)
        {
            var clock = Stopwatch.StartNew();
            var result = QualityRuleEvaluator.Evaluate(rule, data, totalCount);
            clock.Stop();
            result.RuleId = rule.Id;
            result.ExecutedAt = DateTime.UtcNow;

            var record = QualityRuleExecutions.Of(rule.Id, rule, result, tenant.Value, clock.ElapsedMilliseconds);
            var kept = await QualityRuleExecutions.Record(Gateway, QualityServiceTypes.OperationalConnection, record, Logger, ct).ConfigureAwait(false);
            if (kept.IsFailure)
            {
                await Send.ResultAsync(TypedResults.Json(new { Error = "A check ran but its verdict was not recorded", RuleId = rule.Id, Details = kept.CurrentMessage }, statusCode: 500)).ConfigureAwait(false);
                return;
            }

            result.ExecutionId = record.Id;
            results.Add(result);
        }

        await Send.OkAsync(results, ct).ConfigureAwait(false);
    }
}
