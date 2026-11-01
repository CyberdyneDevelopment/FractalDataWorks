using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
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


/// <summary>Endpoint that executes a single quality check by rule identifier.</summary>
public abstract class ExecuteQualityCheckEndpointBase : Endpoint<QualityRuleIdRequest, QualityCheckResultResponse>
{
    /// <summary>Initializes a new instance of the <see cref="ExecuteQualityCheckEndpointBase"/> class.</summary>
        private readonly IQualityRuleConfigurationProvider _provider;
    private readonly IDataGatewayProvider _dataGateways;

    // Why resolved here rather than injected: the gateway is scoped and this is not, so holding one
    // would be a captive dependency. The provider is asked when a call is actually being made.
    private IDataGateway Gateway => _dataGateways.ByName("Main");

    /// <summary>Initializes a new instance of the <see cref="ExecuteQualityCheckEndpointBase"/> class.</summary>
    protected ExecuteQualityCheckEndpointBase(ILogger logger, IQualityRuleConfigurationProvider provider, IDataGatewayProvider dataGateways)
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
        Post("/quality/rules/{Id}/execute");
#if DEVELOP
        AllowAnonymous();
#else
        Policies(ReadPolicy);
#endif
        Summary(s => s.Summary = "Execute a quality check");
    }

    /// <summary>Fetches the quality rule, queries the associated DataSet, and executes the check. Returns Skipped if the rule is disabled, or 404 if the rule is not found.</summary>
    public override async Task HandleAsync(QualityRuleIdRequest req, CancellationToken ct)
    {
        // Why the tenant is read first: a verdict is kept under the caller's active tenant, so a caller with none is refused
        // before any row is read, not after a check has run whose verdict could not be kept.
        var tenant = QualityRuleExecutions.CallerTenant(User, Logger);
        if (tenant.IsFailure)
        {
            await Send.ResultAsync(TypedResults.Json(new { Error = "No active tenant", Details = tenant.CurrentMessage }, statusCode: 400)).ConfigureAwait(false);
            return;
        }

        var ruleResult = await _provider.Get(req.Id, ct).ConfigureAwait(false);

        if (!ruleResult.IsSuccess)
        {
            await Send.ResultAsync(TypedResults.Json(new { Error = "Failed to fetch quality rule", Details = ruleResult.CurrentMessage }, statusCode: 500)).ConfigureAwait(false);
            return;
        }

        if (ruleResult.Value is null)
        {
            await Send.NotFoundAsync(ct).ConfigureAwait(false);
            return;
        }

        var rule = ruleResult.Value;

        if (!rule.IsEnabled)
        {
            await Send.OkAsync(new QualityCheckResultResponse
            {
                RuleId = req.Id,
                Status = "Skipped",
                FailureCount = 0,
                TotalCount = 0,
                ExecutedAt = DateTime.UtcNow,
                ErrorMessage = "Rule is disabled"
            }, ct).ConfigureAwait(false);
            return;
        }

        var named = QualityRuleTarget.RequireDataSet($"Quality rule {req.Id}", rule.DataSetName, Logger);
        if (named.IsFailure)
        {
            await Send.ResultAsync(TypedResults.Json(new { Error = "Quality rule names no data set", Details = named.CurrentMessage }, statusCode: 400)).ConfigureAwait(false);
            return;
        }

        // Why a failed read is the answer and not a 200 whose check "Failed" (FDW-1162): the rule never ran. The read's own
        // code decides the status, and a refusal's reason reaches the caller through the mapper, written once.
        var rows = await QualityRuleTarget.ReadRows(Gateway, rule.DataSetName, Logger, ct).ConfigureAwait(false);
        if (rows.IsFailure || rows.Value is not { } data)
        {
            var (statusCode, problem) = ResultHttpStatusMapper.Map(rows, HttpContext);
            await Send.ResultAsync(TypedResults.Json(problem, statusCode: statusCode)).ConfigureAwait(false);
            return;
        }

        var totalCount = data.Count;

        var clock = Stopwatch.StartNew();
        var result = QualityRuleEvaluator.Evaluate(rule, data, totalCount);
        clock.Stop();
        result.RuleId = req.Id;
        result.ExecutedAt = DateTime.UtcNow;

        // Why a verdict that cannot be kept is the answer and not a 200 (FDW-1363): a check nobody can read back again is
        // the unrecorded "it passed" this feature exists to end.
        var record = QualityRuleExecutions.Of(req.Id, rule, result, tenant.Value, clock.ElapsedMilliseconds);
        var kept = await QualityRuleExecutions.Record(Gateway, QualityServiceTypes.OperationalConnection, record, Logger, ct).ConfigureAwait(false);
        if (kept.IsFailure)
        {
            await Send.ResultAsync(TypedResults.Json(new { Error = "The check ran but its verdict was not recorded", Details = kept.CurrentMessage }, statusCode: 500)).ConfigureAwait(false);
            return;
        }

        result.ExecutionId = record.Id;
        await Send.OkAsync(result, ct).ConfigureAwait(false);
    }
}
