using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Abstractions;
using Fdw.Commands.Data;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Messages;
using Fdw.Operations.Abstractions.Audit;
using Fdw.Operations.Data;
using Fdw.Operations.Execution;
using Fdw.Operations.Logging;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Operations.Audit;

/// <summary>
/// Records administrative changes to the ops.AdministrativeChange table and reads them back within the caller's tenant scope.
/// </summary>
/// <remarks>
/// <para>
/// The actor and the tenant of a record are the caller's own, taken from its authentication context, never stated by the
/// caller. A change that cannot be recorded is a failure the caller returns, so an administrative change never takes effect
/// unaudited. The table is append only: this service inserts and reads, and has no update and no delete.
/// </para>
/// <para>
/// A read is scoped like an execution read (<see cref="ExecutionTenantScope"/>): the platform and the system see every tenant,
/// anyone else sees only the tenant they act in, and a filter can only narrow that.
/// </para>
/// </remarks>
public sealed class AdministrativeAuditService : IAdministrativeAudit
{
    private const string PathName = "ops";
    private const string ContainerName = "AdministrativeChange";

    private readonly IDataGatewayProvider _dataGateways;
    private readonly IAuthenticationContextAccessor _authContext;
    private readonly ILogger _logger;
    private readonly string? _dataStoreName;

    /// <summary>
    /// Initializes a new instance of the <see cref="AdministrativeAuditService"/> class.
    /// </summary>
    /// <param name="dataGateways">Resolves the data gateway when a call is made (the gateway is scoped and this service is not).</param>
    /// <param name="loggerFactory">Creates the service's logger.</param>
    /// <param name="authContext">Supplies the caller, whose identity and tenant every record carries and whose tenant scopes every read.</param>
    /// <param name="dataStoreName">The name of the operational data store, or null when the host has none.</param>
    public AdministrativeAuditService(
        IDataGatewayProvider dataGateways,
        ILoggerFactory loggerFactory,
        IAuthenticationContextAccessor authContext,
        string? dataStoreName)
    {
        _dataGateways = dataGateways ?? throw new ArgumentNullException(nameof(dataGateways));
        _logger = loggerFactory?.CreateLogger<AdministrativeAuditService>()
            ?? throw new ArgumentNullException(nameof(loggerFactory));
        _authContext = authContext ?? throw new ArgumentNullException(nameof(authContext));
        _dataStoreName = dataStoreName;
    }

    // Why resolved here rather than injected: the gateway is scoped and this is not, so holding one would be a captive
    // dependency. The provider is asked when a call is actually being made.
    private IDataGateway Gateway => _dataGateways.ByName("Main");

    // Null when the host names no operational store; each entry point reports that, so none reaches a gateway without a target.
    private DataStoreTarget? StoreTarget
        => string.IsNullOrWhiteSpace(_dataStoreName) ? null : new DataStoreTarget(_dataStoreName, PathName, ContainerName);

    /// <inheritdoc />
    public async Task<IGenericResult> Record(AdministrativeChangeEntry entry, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        // Reported here rather than refused at registration: a host that records no administrative change is not
        // misconfigured for having no operational store.
        if (StoreTarget is not { } target)
            return GenericResult.Failure(OperationsLog.OperationalConnectionNotSet(_logger));

        if (EntryProblem(entry) is { } problem)
            return GenericResult.Failure(problem);

        if (_authContext.Current is not { IsAuthenticated: true } caller
            || !Guid.TryParse(caller.UserId, out var actorUserId)
            || string.IsNullOrWhiteSpace(caller.Username))
            return GenericResult.Failure(AdministrativeAuditLog.ActorRequired(_logger, entry.ChangeKind));

        if (caller.ActiveTenantId is not { } tenantId)
            return GenericResult.Failure(AdministrativeAuditLog.TenantRequired(_logger, entry.ChangeKind));

        var row = new AdministrativeChange
        {
            ActorUserId = actorUserId,
            ActorName = caller.Username,
            ChangeKind = entry.ChangeKind,
            ItemKind = entry.ItemKind,
            ItemId = entry.ItemId,
            Level = entry.Level.Name,
            TenantId = tenantId,
            OrganizationId = entry.OrganizationId,
            OldValue = entry.OldValue,
            NewValue = entry.NewValue,
            Reason = entry.Reason
        };

        var inserted = await Gateway.Execute<int>(new InsertCommand<AdministrativeChange>(row), target, cancellationToken).ConfigureAwait(false);
        if (!inserted.IsSuccess)
            return GenericResult.Failure(AdministrativeAuditLog.StoreFailed(_logger, entry.ChangeKind, entry.ItemId, inserted.CurrentMessage));

        AdministrativeAuditLog.Recorded(_logger, entry.ChangeKind, entry.ItemKind, entry.ItemId, row.Level, row.ActorName);
        return GenericResult.Success();
    }

    /// <inheritdoc />
    public async Task<IGenericResult<IPagedResponse<AdministrativeChangeRecord>>> Read(
        AdministrativeChangeFilter filter,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter);

        if (StoreTarget is not { } target)
            return GenericResult<IPagedResponse<AdministrativeChangeRecord>>.Failure(OperationsLog.OperationalConnectionNotSet(_logger));

        if (filter.Page < 1 || filter.PageSize < 1)
            return GenericResult<IPagedResponse<AdministrativeChangeRecord>>.Failure(AdministrativeAuditLog.PageInvalid(_logger, filter.Page, filter.PageSize));

        var scope = ExecutionTenantScope.For(_authContext.Current);
        if (scope.ReachesNothing || (filter.TenantId is { } asked && !scope.Reaches(asked)))
            return GenericResult<IPagedResponse<AdministrativeChangeRecord>>.Success(EmptyPage(filter));

        var query = new QueryCommand<AdministrativeChange>
        {
            Ordering = new OrderingExpression
            {
                OrderedFields = [new OrderedField { PropertyName = "OccurredAt", Direction = SortDirections.Descending }]
            },
            Paging = new PagingExpression { Skip = (filter.Page - 1) * filter.PageSize, Take = filter.PageSize },
            Filter = FilterFor(filter, scope)
        };

        var result = await Gateway.Execute<IEnumerable<AdministrativeChange>>(query, target, cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess || result.Value is not { } rows)
            return GenericResult<IPagedResponse<AdministrativeChangeRecord>>.Failure(
                AdministrativeAuditLog.ReadFailed(_logger, result.IsSuccess ? "the read answered no rows collection" : result.CurrentMessage));

        var records = rows.Select(ToRecord).ToList();
        return GenericResult<IPagedResponse<AdministrativeChangeRecord>>.Success(
            new PagedResponse<AdministrativeChangeRecord>(records, filter.Page, filter.PageSize, EstimateTotal(filter, records.Count)));
    }

    private IGenericMessage? EntryProblem(AdministrativeChangeEntry entry)
    {
        if (string.IsNullOrWhiteSpace(entry.ChangeKind))
            return AdministrativeAuditLog.FieldRequired(_logger, nameof(entry.ChangeKind));

        if (string.IsNullOrWhiteSpace(entry.ItemKind))
            return AdministrativeAuditLog.FieldRequired(_logger, nameof(entry.ItemKind));

        if (string.IsNullOrWhiteSpace(entry.ItemId))
            return AdministrativeAuditLog.FieldRequired(_logger, nameof(entry.ItemId));

        if (entry.Level.Depth < 0)
            return AdministrativeAuditLog.LevelUnknown(_logger, entry.ChangeKind);

        var atOrganization = entry.Level.Depth == AdministrativeLevels.ByName("Organization").Depth;
        if (atOrganization && entry.OrganizationId is null)
            return AdministrativeAuditLog.OrganizationRequired(_logger, entry.ChangeKind);

        return !atOrganization && entry.OrganizationId is not null
            ? AdministrativeAuditLog.OrganizationNotAllowed(_logger, entry.ChangeKind, entry.Level.Name)
            : null;
    }

    // The terms of a read: the tenant (the caller's own unless the caller reaches every tenant and asked for one), then the
    // narrowing terms the caller asked for.
    private static FilterExpression? FilterFor(AdministrativeChangeFilter filter, ExecutionTenantScope scope)
    {
        var conditions = new List<FilterCondition>();
        var tenant = scope.ReachesAll ? filter.TenantId : scope.TenantId;
        AddEqual(conditions, "TenantId", tenant);
        AddEqual(conditions, "ActorUserId", filter.ActorUserId);
        AddEqual(conditions, "OrganizationId", filter.OrganizationId);
        AddCompare(conditions, "OccurredAt", "GreaterThanOrEqual", filter.From);
        AddCompare(conditions, "OccurredAt", "LessThan", filter.To);

        return conditions.Count switch
        {
            0 => null,
            1 => new FilterExpression { Root = conditions[0] },
            _ => new FilterExpression { Root = new FilterGroup { Operator = LogicalOperator.And, Nodes = [.. conditions] } }
        };
    }

    private static void AddEqual(List<FilterCondition> conditions, string property, Guid? value)
    {
        if (value is { } stated)
            conditions.Add(new FilterCondition { PropertyName = property, Operator = FilterOperators.ByName("Equal"), Value = stated });
    }

    private static void AddCompare(List<FilterCondition> conditions, string property, string operatorName, DateTimeOffset? value)
    {
        if (value is { } stated)
            conditions.Add(new FilterCondition { PropertyName = property, Operator = FilterOperators.ByName(operatorName), Value = stated });
    }

    private static AdministrativeChangeRecord ToRecord(AdministrativeChange row)
        => new(row.Id, row.OccurredAt, row.ActorUserId, row.ActorName, row.ChangeKind, row.ItemKind, row.ItemId, row.Level,
            row.TenantId, row.OrganizationId, row.OldValue, row.NewValue, row.Reason);

    private static PagedResponse<AdministrativeChangeRecord> EmptyPage(AdministrativeChangeFilter filter)
        => new(new List<AdministrativeChangeRecord>(), filter.Page, filter.PageSize, 0);

    // Why an estimate: the read asks for one page and does not count the rest, so a full page is taken to mean there is at
    // least one more, as the execution list does.
    private static long EstimateTotal(AdministrativeChangeFilter filter, int count)
        => count < filter.PageSize
            ? (long)(filter.Page - 1) * filter.PageSize + count
            : (long)(filter.Page + 1) * filter.PageSize;
}
