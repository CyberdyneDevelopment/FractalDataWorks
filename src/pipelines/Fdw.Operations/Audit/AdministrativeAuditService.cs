using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Abstractions;
using Fdw.Claims.Abstractions;
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
    private readonly IUserNames _userNames;
    private readonly ILogger _logger;
    private readonly string? _dataStoreName;

    /// <summary>
    /// Initializes a new instance of the <see cref="AdministrativeAuditService"/> class.
    /// </summary>
    /// <param name="dataGateways">Resolves the data gateway when a call is made (the gateway is scoped and this service is not).</param>
    /// <param name="loggerFactory">Creates the service's logger.</param>
    /// <param name="authContext">Supplies the caller, whose identity and tenant every record carries and whose tenant scopes every read.</param>
    /// <param name="userNames">Reads the caller's user name, which every record carries as the actor (FDW-2199).</param>
    /// <param name="dataStoreName">The name of the operational data store, or null when the host has none.</param>
    public AdministrativeAuditService(
        IDataGatewayProvider dataGateways,
        ILoggerFactory loggerFactory,
        IAuthenticationContextAccessor authContext,
        IUserNames userNames,
        string? dataStoreName)
    {
        _dataGateways = dataGateways;
        _logger = loggerFactory?.CreateLogger<AdministrativeAuditService>()
            ?? throw new ArgumentNullException(nameof(loggerFactory));
        _authContext = authContext;
        _userNames = userNames;
        _dataStoreName = dataStoreName;
    }

    // Why resolved here rather than injected: the gateway is scoped and this is not, so holding one would be a captive
    // dependency. The provider is asked when a call is actually being made.
    private IDataGateway Gateway => _dataGateways.ByName(DataGatewayNames.Main);

    // Null when the host names no operational store; each entry point reports that, so none reaches a gateway without a target.
    private DataStoreTarget? StoreTarget
        => string.IsNullOrWhiteSpace(_dataStoreName) ? null : new DataStoreTarget(_dataStoreName, PathName, ContainerName);

    /// <inheritdoc />
    public async Task<IGenericResult> Record(AdministrativeChangeEntry entry, CancellationToken cancellationToken = default)
    {
        // Reported here rather than refused at registration: a host that records no administrative change is not
        // misconfigured for having no operational store.
        if (StoreTarget is not { } target)
            return GenericResult.Failure(OperationsLog.OperationalConnectionNotSet(_logger));

        var row = await RowFor(entry, cancellationToken).ConfigureAwait(false);
        if (!row.IsSuccess)
            return (IGenericResult)row;

        var inserted = await Gateway.Execute<int>(new InsertCommand<AdministrativeChange>(row.Value!), target, cancellationToken).ConfigureAwait(false);
        if (!inserted.IsSuccess)
            return GenericResult.Failure(AdministrativeAuditLog.StoreFailed(_logger, entry.ChangeKind, entry.ItemId, inserted.CurrentMessage));

        AdministrativeAuditLog.Recorded(_logger, entry.ChangeKind, entry.ItemKind, entry.ItemId, row.Value!.Level, row.Value.ActorName);
        return GenericResult.Success();
    }

    /// <inheritdoc />
    public async Task<IGenericResult> RecordAll(IReadOnlyList<AdministrativeChangeEntry> entries, CancellationToken cancellationToken = default)
    {
        if (StoreTarget is not { } target)
            return GenericResult.Failure(OperationsLog.OperationalConnectionNotSet(_logger));
        if (entries.Count == 0)
            return GenericResult.Failure(AdministrativeAuditLog.EntriesRequired(_logger));

        // Every entry is checked and turned into its row before anything is written, so one incomplete entry writes none, and the
        // whole change goes in one insert, so the store takes all of it or none of it.
        var rows = new List<AdministrativeChange>(entries.Count);
        foreach (var entry in entries)
        {
            var row = await RowFor(entry, cancellationToken).ConfigureAwait(false);
            if (!row.IsSuccess)
                return (IGenericResult)row;
            rows.Add(row.Value!);
        }

        var inserted = await Gateway.Execute<int>(new InsertCommand<AdministrativeChange>(rows), target, cancellationToken).ConfigureAwait(false);
        if (!inserted.IsSuccess)
            return GenericResult.Failure(AdministrativeAuditLog.StoreFailed(_logger, entries[0].ChangeKind, entries[0].ItemId, inserted.CurrentMessage));

        foreach (var row in rows)
            AdministrativeAuditLog.Recorded(_logger, row.ChangeKind, row.ItemKind, row.ItemId, row.Level, row.ActorName);
        return GenericResult.Success();
    }

    // The checks every entry goes through, then the row under the caller's identity and tenant. Shared by Record and RecordAll so a
    // change of several members is held to the same rules as a change of one.
    private async Task<IGenericResult<AdministrativeChange>> RowFor(AdministrativeChangeEntry entry, CancellationToken cancellationToken)
    {
        if (EntryProblem(entry) is { } problem)
            return GenericResult<AdministrativeChange>.Failure(problem);

        if (_authContext.Current is not { IsAuthenticated: true } caller
            || !Guid.TryParse(caller.UserId, out var actorUserId))
            return GenericResult<AdministrativeChange>.Failure(AdministrativeAuditLog.ActorRequired(_logger, entry.ChangeKind));

        if (caller.ActiveTenantId is not { } tenantId)
            return GenericResult<AdministrativeChange>.Failure(AdministrativeAuditLog.TenantRequired(_logger, entry.ChangeKind));

        // Why asked of the account and not read from the context: the token carries the user id, and the context's Username falls back to it when no name claim is
        // there, which recorded an id as the actor's name (FDW-2199). A person with no account, or no name, is a failure here, not a record with a stand-in.
        var actorName = await _userNames.Get(actorUserId, cancellationToken).ConfigureAwait(false);
        if (!actorName.IsSuccess)
            return actorName.ToNewResult<AdministrativeChange>();

        return GenericResult<AdministrativeChange>.Success(new AdministrativeChange
        {
            ActorUserId = actorUserId,
            ActorName = actorName.Value!,
            ChangeKind = entry.ChangeKind,
            ItemKind = entry.ItemKind,
            ItemId = entry.ItemId,
            Level = entry.Level.Name,
            TenantId = tenantId,
            OrganizationId = entry.OrganizationId,
            OldValue = entry.OldValue,
            NewValue = entry.NewValue,
            Reason = entry.Reason,
            // The person is the actor above; the role is the one their session acts with, read from the same context, never stated by the caller.
            ActingRole = caller.Claims.TryGetValue(ClaimDefinitions.actingRole.Name, out var acting) ? acting as string : null
        });
    }

    /// <inheritdoc />
    public async Task<IGenericResult<IPagedResponse<AdministrativeChangeRecord>>> Read(
        AdministrativeChangeFilter filter,
        CancellationToken cancellationToken = default)
    {
        if (StoreTarget is not { } target)
            return GenericResult<IPagedResponse<AdministrativeChangeRecord>>.Failure(OperationsLog.OperationalConnectionNotSet(_logger));

        if (filter.Page < 1 || filter.PageSize < 1)
            return GenericResult<IPagedResponse<AdministrativeChangeRecord>>.Failure(AdministrativeAuditLog.PageInvalid(_logger, filter.Page, filter.PageSize));

        var scope = ExecutionTenantScope.For(_authContext.Current);
        if (scope.ReachesNothing || (filter.TenantId is { } asked && !scope.Reaches(asked)))
            return GenericResult<IPagedResponse<AdministrativeChangeRecord>>.Success(EmptyPage(filter));

        if (filter.NewestOfEachItem)
            return await ReadNewestOfEachItem(filter, scope, target, cancellationToken).ConfigureAwait(false);

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
        AddEqual(conditions, "ItemKind", filter.ItemKind);
        AddEqual(conditions, "ItemId", filter.ItemId);
        AddIn(conditions, "ItemId", filter.ItemIds);
        AddCompare(conditions, "OccurredAt", "GreaterThanOrEqual", filter.From);
        AddCompare(conditions, "OccurredAt", "LessThan", filter.To);

        return FilterGroup.AllOf(conditions) is { } root ? new FilterExpression { Root = root } : null;
    }

    private static void AddEqual(List<FilterCondition> conditions, string property, Guid? value)
    {
        if (value is { } stated)
            conditions.Add(new FilterCondition { PropertyName = property, Operator = FilterOperators.ByName("Equal"), Value = stated });
    }

    private static void AddEqual(List<FilterCondition> conditions, string property, string? value)
    {
        if (value is not null)
            conditions.Add(new FilterCondition { PropertyName = property, Operator = FilterOperators.ByName("Equal"), Value = value });
    }

    private static void AddIn(List<FilterCondition> conditions, string property, IReadOnlyList<string>? values)
    {
        if (values is not null)
            conditions.Add(new FilterCondition { PropertyName = property, Operator = FilterOperators.ByName("In"), Value = values });
    }

    private const string NewestColumn = "NewestAt";

    // The newest change of each item named, in two set-based reads: the newest time of each item (a group by in the store), then every change of the
    // items at those times, so a page of items is answered with two queries and never one for each item.
    private async Task<IGenericResult<IPagedResponse<AdministrativeChangeRecord>>> ReadNewestOfEachItem(
        AdministrativeChangeFilter filter, ExecutionTenantScope scope, DataStoreTarget target, CancellationToken cancellationToken)
    {
        if (filter.ItemKind is null)
            return GenericResult<IPagedResponse<AdministrativeChangeRecord>>.Failure(AdministrativeAuditLog.FieldRequired(_logger, nameof(filter.ItemKind)));
        if (filter.ItemIds is not { Count: > 0 })
            return GenericResult<IPagedResponse<AdministrativeChangeRecord>>.Success(EmptyPage(filter));

        var newest = new QueryCommand<Dictionary<string, object?>>
        {
            Filter = FilterFor(filter, scope),
            Aggregation = new AggregationExpression
            {
                GroupByFields = ["ItemId"],
                Aggregations = new Dictionary<string, string>(StringComparer.Ordinal) { [NewestColumn] = "MAX(OccurredAt)" },
            },
        };
        var times = await Gateway.Execute<IEnumerable<Dictionary<string, object?>>>(newest, target, cancellationToken).ConfigureAwait(false);
        if (!times.IsSuccess || times.Value is not { } timeRows)
            return GenericResult<IPagedResponse<AdministrativeChangeRecord>>.Failure(AdministrativeAuditLog.ReadFailed(_logger, "the newest time of each item could not be read"));

        var pairs = NewestPairs(timeRows);
        if (pairs is null)
            return GenericResult<IPagedResponse<AdministrativeChangeRecord>>.Failure(AdministrativeAuditLog.ReadFailed(_logger, "the newest time of an item did not read as a time"));
        if (pairs.Nodes.Count == 0)
            return GenericResult<IPagedResponse<AdministrativeChangeRecord>>.Success(EmptyPage(filter));

        var changes = new QueryCommand<AdministrativeChange>
        {
            Filter = new FilterExpression { Root = new FilterGroup { Operator = LogicalOperator.And, Nodes = [FilterFor(filter, scope)!.Root!, pairs] } },
            Ordering = new OrderingExpression { OrderedFields = [new OrderedField { PropertyName = "OccurredAt", Direction = SortDirections.Descending }] },
        };
        var result = await Gateway.Execute<IEnumerable<AdministrativeChange>>(changes, target, cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess || result.Value is not { } rows)
            return GenericResult<IPagedResponse<AdministrativeChangeRecord>>.Failure(AdministrativeAuditLog.ReadFailed(_logger, result.IsSuccess ? "the read answered no rows collection" : result.CurrentMessage));

        var records = rows.Select(ToRecord).ToList();
        return GenericResult<IPagedResponse<AdministrativeChangeRecord>>.Success(new PagedResponse<AdministrativeChangeRecord>(records, 1, Math.Max(1, filter.ItemIds.Count), records.Count));
    }

    // An item at its newest time, for each row of the group by; one of them is true for a change recorded at exactly that time. Null when a row does not
    // say its item or its time as it should: a read that cannot be trusted is never answered as nothing.
    private static FilterGroup? NewestPairs(IEnumerable<Dictionary<string, object?>> rows)
    {
        List<IFilterNode> either = [];
        foreach (var row in rows)
        {
            if (!row.TryGetValue("ItemId", out var item) || item is not string itemId || !row.TryGetValue(NewestColumn, out var at) || at is not DateTimeOffset moment)
                return null;

            either.Add(new FilterGroup { Operator = LogicalOperator.And, Nodes = [Equal("ItemId", itemId), Equal("OccurredAt", moment)] });
        }

        return new FilterGroup { Operator = LogicalOperator.Or, Nodes = either };
    }

    private static FilterCondition Equal(string property, object value)
        => new() { PropertyName = property, Operator = FilterOperators.ByName("Equal"), Value = value };

    private static void AddCompare(List<FilterCondition> conditions, string property, string operatorName, DateTimeOffset? value)
    {
        if (value is { } stated)
            conditions.Add(new FilterCondition { PropertyName = property, Operator = FilterOperators.ByName(operatorName), Value = stated });
    }

    private static AdministrativeChangeRecord ToRecord(AdministrativeChange row)
        => new(row.Id, row.OccurredAt, row.ActorUserId, row.ActorName, row.ChangeKind, row.ItemKind, row.ItemId, row.Level,
            row.TenantId, row.OrganizationId, row.OldValue, row.NewValue, row.Reason, row.ActingRole);

    private static PagedResponse<AdministrativeChangeRecord> EmptyPage(AdministrativeChangeFilter filter)
        => new(new List<AdministrativeChangeRecord>(), filter.Page, filter.PageSize, 0);

    // Why an estimate: the read asks for one page and does not count the rest, so a full page is taken to mean there is at
    // least one more, as the execution list does.
    private static long EstimateTotal(AdministrativeChangeFilter filter, int count)
        => count < filter.PageSize
            ? (long)(filter.Page - 1) * filter.PageSize + count
            : (long)(filter.Page + 1) * filter.PageSize;
}
