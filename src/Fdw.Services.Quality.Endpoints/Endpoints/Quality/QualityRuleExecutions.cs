using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Extensions;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Quality.Configuration;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Quality.Endpoints;

/// <summary>
/// Records and reads the verdicts of quality rule executions in the operational store (<c>dq.QualityRuleExecution</c>) (FDW-1363).
/// </summary>
/// <remarks>
/// <para>
/// Why a static class over the gateway, as <c>PipelineWatermarks</c> does for the run watermark: the verdicts are operational
/// rows written through the same store and path, and need no service of their own. The store name is the host's
/// (<see cref="QualityServiceTypes.OperationalConnection"/>); nothing here names a store.
/// </para>
/// <para>
/// Nothing is defaulted. A check that has no active tenant to record under, or a host that names no operational store, is a
/// failure the caller is told about, never a check whose verdict is quietly dropped.
/// </para>
/// </remarks>
public static class QualityRuleExecutions
{
    /// <summary>The path (schema) of the executions table in the operational store.</summary>
    public const string PathName = "dq";

    /// <summary>The container (table) that holds the executions.</summary>
    public const string ContainerName = "QualityRuleExecution";

    /// <summary>The most executions one read answers.</summary>
    public const int MaxTake = 500;

    /// <summary>Reads the tenant the caller is working in.</summary>
    /// <param name="user">The caller's claims.</param>
    /// <param name="logger">The logger.</param>
    /// <returns>The active tenant, or a failure that says the caller has none.</returns>
    public static IGenericResult<Guid> CallerTenant(ClaimsPrincipal user, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(user);

        return new ClaimsPrincipalAuthenticationContext(user).ActiveTenantId is { } tenant && tenant != Guid.Empty
            ? GenericResult<Guid>.Success(tenant)
            : GenericResult<Guid>.Failure(QualityCheckEndpointLog.NoActiveTenant(logger));
    }

    /// <summary>Shapes the row that records one rule's verdict.</summary>
    /// <param name="ruleId">The identifier of the rule that ran.</param>
    /// <param name="rule">The rule that ran.</param>
    /// <param name="result">The verdict the evaluator answered.</param>
    /// <param name="tenantId">The tenant the check ran for.</param>
    /// <param name="durationMs">How long the evaluation took.</param>
    /// <returns>The row, ready to record.</returns>
    public static QualityRuleExecutionRecord Of(Guid ruleId, IQualityRuleImplementationConfiguration rule, QualityCheckResultResponse result, Guid tenantId, long durationMs)
    {
        ArgumentNullException.ThrowIfNull(rule);
        ArgumentNullException.ThrowIfNull(result);

        // Why a rule that could not run judges no rows: its ErrorMessage says why, and every row is "not judged", so the
        // counts never read as a pass.
        var judged = result.ErrorMessage is null && result.Status is "Passed" or "Failed";
        return new QualityRuleExecutionRecord
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenantId,
            QualityRuleId = ruleId,
            DataSetName = rule.DataSetName,
            FieldName = rule.FieldName,
            RuleType = rule.RuleType,
            Severity = rule.Severity,
            Outcome = result.Status,
            ExecutedAt = new DateTimeOffset(DateTime.SpecifyKind(result.ExecutedAt, DateTimeKind.Utc)),
            PassCount = judged ? Math.Max(0, result.TotalCount - result.FailureCount) : 0,
            FailCount = judged ? result.FailureCount : 0,
            SkipCount = judged ? 0 : result.TotalCount,
            ExecutionDurationMs = durationMs,
            ErrorMessage = result.ErrorMessage,
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }

    /// <summary>Appends one verdict to the operational store.</summary>
    /// <param name="gateway">The data gateway.</param>
    /// <param name="operationalStore">The operational store the host names.</param>
    /// <param name="record">The verdict to keep.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Success, or the failure that says the check ran but its verdict was not kept.</returns>
    public static async Task<IGenericResult> Record(
        IDataGateway gateway,
        string? operationalStore,
        QualityRuleExecutionRecord record,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(gateway);
        ArgumentNullException.ThrowIfNull(record);

        if (string.IsNullOrWhiteSpace(operationalStore))
            return GenericResult.Failure(QualityCheckEndpointLog.ExecutionStoreNotNamed(logger));

        var written = await gateway.Execute<int>(
            Insert.Into<QualityRuleExecutionRecord>(ContainerName).DataStore(operationalStore).Path(PathName).Value(record),
            cancellationToken).ConfigureAwait(false);
        if (written.IsFailure)
            return GenericResult.Failure(QualityCheckEndpointLog.ExecutionNotRecorded(logger, record.QualityRuleId, written.CurrentMessage));

        QualityCheckEndpointLog.ExecutionRecorded(logger, record.QualityRuleId, record.DataSetName, record.Outcome);
        return GenericResult.Success();
    }

    /// <summary>Reads the newest executions of one rule.</summary>
    /// <param name="gateway">The data gateway.</param>
    /// <param name="operationalStore">The operational store the host names.</param>
    /// <param name="tenantId">The tenant.</param>
    /// <param name="ruleId">The rule.</param>
    /// <param name="take">How many to answer, from 1 to <see cref="MaxTake"/>.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The executions, newest first, or the failure that says they were not read.</returns>
    public static async Task<IGenericResult<IReadOnlyList<QualityRuleExecutionRecord>>> ForRule(
        IDataGateway gateway,
        string? operationalStore,
        Guid tenantId,
        Guid ruleId,
        int take,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(gateway);

        if (string.IsNullOrWhiteSpace(operationalStore))
            return GenericResult<IReadOnlyList<QualityRuleExecutionRecord>>.Failure(QualityCheckEndpointLog.ExecutionStoreNotNamed(logger));

        var read = await gateway.Execute<IEnumerable<QualityRuleExecutionRecord>>(
            Query.From<QualityRuleExecutionRecord>(operationalStore, PathName, ContainerName)
                .Where("TenantId", tenantId)
                .Where("QualityRuleId", ruleId)
                .OrderByDescending("ExecutedAt")
                .Paging(0, take)
                .Build(),
            cancellationToken).ConfigureAwait(false);
        return read.IsFailure
            ? GenericResult<IReadOnlyList<QualityRuleExecutionRecord>>.Failure(
                QualityCheckEndpointLog.ExecutionsNotRead(logger, ruleId.ToString(), read.CurrentMessage))
            : GenericResult<IReadOnlyList<QualityRuleExecutionRecord>>.Success(read.Value!.ToList());
    }

    /// <summary>Shapes a stored row for the wire.</summary>
    /// <param name="record">The stored row.</param>
    /// <returns>The response.</returns>
    public static QualityRuleExecutionResponse ToResponse(QualityRuleExecutionRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        return new QualityRuleExecutionResponse
        {
            Id = record.Id,
            QualityRuleId = record.QualityRuleId,
            DataSetName = record.DataSetName,
            FieldName = record.FieldName,
            RuleType = record.RuleType,
            Severity = record.Severity,
            Outcome = record.Outcome,
            ExecutedAt = record.ExecutedAt,
            PassCount = record.PassCount,
            FailCount = record.FailCount,
            SkipCount = record.SkipCount,
            ExecutionDurationMs = record.ExecutionDurationMs,
            ErrorMessage = record.ErrorMessage,
        };
    }
}
