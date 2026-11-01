using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Abstractions;
using Fdw.Commands.Data;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Operations.Abstractions;
using Fdw.Operations.Abstractions.Execution;
using Fdw.Operations.Abstractions.TypeCollections.Execution;
using Fdw.Operations.Abstractions.TypeCollections.ExecutionStateOptions;
using Fdw.Operations.Data;
using Fdw.Operations.Logging;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Notifications;
using Fdw.Services.Notifications.Abstractions;
using Fdw.Services.Notifications.Configuration;
using Fdw.Services.Notifications.Results;
using Fdw.ServiceTypes;
using Microsoft.Extensions.Logging;

namespace Fdw.Operations.Execution;

/// <summary>
/// Service for tracking execution of workflows, jobs, and other hierarchical items.
/// Uses DataGateway to persist to the ops schema.
/// </summary>
public sealed class ExecutionTrackingService : IExecutionTracker
{
    private const string PathName = "ops";
    private const string ContainerNameExecutionItem = "ExecutionItem";
    private const string ContainerNameExecutionEvent = "ExecutionEvent";

    private readonly IDataGatewayProvider _dataGateways;
    private readonly IAuthenticationContextAccessor _authContext;

    // Why resolved here rather than injected: the gateway is scoped and this is not, so holding one
    // would be a captive dependency. The provider is asked when a call is actually being made.
    private IDataGateway Gateway => _dataGateways.ByName("Main");
    private readonly ILogger _logger;
    private readonly string? _dataStoreName;
    private readonly INotificationServiceProvider? _notificationProvider;
    private readonly INotificationRuleConfigurationProvider? _notificationRuleProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExecutionTrackingService"/> class.
    /// </summary>
    /// <param name="dataGateways">The data gateway for persistence.</param>
    /// <param name="loggerFactory">Logger factory.</param>
    /// <param name="authContext">Supplies the caller, whose tenant scopes every read and control (see <see cref="ExecutionTenantScope"/>).</param>
    /// <param name="dataStoreName">
    /// The store the tracker reads and writes, or null when the host has not named one. Null is
    /// accepted rather than rejected here because every host registers the Operations option as part
    /// of the platform bootstrap, including hosts that never track an execution; refusing construction
    /// would stop those hosts booting. Each operation reports it instead.
    /// </param>
    /// <param name="notificationProvider">Optional notification service provider. When null, notification emission is skipped.</param>
    /// <param name="notificationRuleProvider">Optional notification rule configuration provider. When null, notification emission is skipped.</param>
    public ExecutionTrackingService(
        IDataGatewayProvider dataGateways,
        ILoggerFactory loggerFactory,
        IAuthenticationContextAccessor authContext,
        string? dataStoreName,
        INotificationServiceProvider? notificationProvider = null,
        INotificationRuleConfigurationProvider? notificationRuleProvider = null)
    {
        _dataGateways = dataGateways ?? throw new ArgumentNullException(nameof(dataGateways));
        _logger = loggerFactory?.CreateLogger<ExecutionTrackingService>()
            ?? throw new ArgumentNullException(nameof(loggerFactory));
        _authContext = authContext ?? throw new ArgumentNullException(nameof(authContext));
        _dataStoreName = dataStoreName;
        _notificationProvider = notificationProvider;
        _notificationRuleProvider = notificationRuleProvider;
    }

    /// <summary>
    /// Whether the host named the store this tracker reads and writes.
    /// </summary>
    private bool StoreIsNamed => !string.IsNullOrWhiteSpace(_dataStoreName);

    /// <summary>
    /// Builds a target in the host's operational store.
    /// </summary>
    /// <remarks>
    /// Every public entry point checks <see cref="StoreIsNamed"/> and returns a failure before
    /// reaching here, which is what makes the suppression sound rather than hopeful.
    /// </remarks>
    private DataStoreTarget TargetFor(string container) => new(_dataStoreName!, PathName, container);

    /// <inheritdoc />
    public async Task<IGenericResult<IExecutionItem>> CreateItem(
        IExecutionItemType itemType,
        string name,
        Guid? tenantId,
        Guid? parentId = null,
        string? correlationId = null,
        string? triggerSource = null,
        IReadOnlyDictionary<string, object?>? parameters = null,
        CancellationToken cancellationToken = default)
    {
        // Reported here rather than refused at registration: a host that never tracks an
        // execution is not misconfigured for having no operational store.
        if (!StoreIsNamed)
            return GenericResult<IExecutionItem>.Failure(OperationsLog.OperationalConnectionNotSet(_logger));

        if (string.IsNullOrWhiteSpace(name))
        {
            return GenericResult<IExecutionItem>.Failure(
                OperationsLog.ExecutionItemNameRequired(_logger));
        }

        var parentValidation = await ValidateParent(itemType, name, parentId, cancellationToken).ConfigureAwait(false);
        if (!parentValidation.IsSuccess)
        {
            return parentValidation.ToNewResult<IExecutionItem>();
        }

        var (rootId, parentTenantId) = parentValidation.Value;

        var tenantResult = ResolveTenant(name, tenantId, parentId, parentTenantId);
        if (!tenantResult.IsSuccess)
        {
            return tenantResult.ToNewResult<IExecutionItem>();
        }

        var poco = ExecutionItemRecord.CreatePoco(itemType, name, parentId, rootId, tenantResult.Value, correlationId, triggerSource, parameters);

        if (!parentId.HasValue)
        {
            poco.RootExecutionItemId = poco.Id;
        }

        var persistResult = await PersistNewItem(poco, cancellationToken).ConfigureAwait(false);
        if (!persistResult.IsSuccess)
        {
            return persistResult.ToNewResult<IExecutionItem>();
        }

        await RecordEventInternal(
            poco.Id, 1, "Created", null, poco.State,
            $"Execution item '{name}' created", null, triggerSource,
            cancellationToken).ConfigureAwait(false);

        OperationsLog.ExecutionItemCreated(_logger, poco.Id, name, itemType.Name);

        return GenericResult<IExecutionItem>.Success(new ExecutionItemRecord(poco));
    }

    /// <inheritdoc />
    public async Task<IGenericResult> TransitionState(
        Guid executionItemId,
        IExecutionStateType newState,
        string? message = null,
        string? actor = null,
        CancellationToken cancellationToken = default)
    {
        // Reported here rather than refused at registration: a host that never tracks an
        // execution is not misconfigured for having no operational store.
        if (!StoreIsNamed)
            return GenericResult.Failure(OperationsLog.OperationalConnectionNotSet(_logger));

        var itemResult = await GetItemInternal(executionItemId, cancellationToken).ConfigureAwait(false);
        if (!itemResult.IsSuccess)
        {
            return GenericResult.Failure(
                OperationsLog.ExecutionItemNotFound(_logger, executionItemId));
        }

        var item = itemResult.Value!;
        var currentState = ExecutionStateTypes.ByName(item.State);

        var validationResult = ValidateStateTransition(executionItemId, currentState, newState);
        if (!validationResult.IsSuccess)
        {
            return validationResult;
        }

        var sequenceNumber = await GetNextSequenceNumber(executionItemId, cancellationToken).ConfigureAwait(false);

        item.State = newState.Name;
        ApplyTimingFields(item, newState);

        var updateResult = await PersistItemUpdate(item, executionItemId, cancellationToken).ConfigureAwait(false);
        if (!updateResult.IsSuccess)
        {
            return updateResult;
        }

        await RecordEventInternal(
            executionItemId, sequenceNumber, "StateChange",
            currentState.Name, newState.Name, message, null, actor,
            cancellationToken).ConfigureAwait(false);

        OperationsLog.StateTransitionRecorded(_logger, executionItemId, currentState.Name, newState.Name);

        return GenericResult.Success();
    }

    /// <inheritdoc />
    public async Task<IGenericResult<IExecutionEvent>> RecordEvent(
        Guid executionItemId,
        string eventType,
        string? message = null,
        IReadOnlyDictionary<string, object?>? data = null,
        string? actor = null,
        CancellationToken cancellationToken = default)
    {
        // Reported here rather than refused at registration: a host that never tracks an
        // execution is not misconfigured for having no operational store.
        if (!StoreIsNamed)
            return GenericResult<IExecutionEvent>.Failure(OperationsLog.OperationalConnectionNotSet(_logger));

        // Verify item exists
        var itemResult = await GetItemInternal(executionItemId, cancellationToken).ConfigureAwait(false);
        if (!itemResult.IsSuccess)
        {
            return GenericResult<IExecutionEvent>.Failure(
                OperationsLog.ExecutionItemNotFound(_logger, executionItemId));
        }

        var sequenceNumber = await GetNextSequenceNumber(executionItemId, cancellationToken).ConfigureAwait(false);

        var eventPoco = await RecordEventInternal(
            executionItemId,
            sequenceNumber,
            eventType,
            null,
            null,
            message,
            data,
            actor,
            cancellationToken).ConfigureAwait(false);

        if (eventPoco == null)
        {
            return GenericResult<IExecutionEvent>.Failure(
                OperationsLog.ExecutionItemPersistFailed(_logger, executionItemId, "Failed to record event"));
        }

        OperationsLog.EventRecorded(_logger, executionItemId, eventType);

        return GenericResult<IExecutionEvent>.Success(new ExecutionEventRecord(eventPoco));
    }

    /// <inheritdoc />
    public async Task<IGenericResult> Complete(
        Guid executionItemId,
        bool success,
        string? resultCode = null,
        string? resultMessage = null,
        CancellationToken cancellationToken = default)
    {
        // Reported here rather than refused at registration: a host that never tracks an
        // execution is not misconfigured for having no operational store.
        if (!StoreIsNamed)
            return GenericResult.Failure(OperationsLog.OperationalConnectionNotSet(_logger));

        IExecutionStateType targetState = success ? ExecutionStateTypes.Completed : ExecutionStateTypes.Failed;

        var itemResult = await GetItemInternal(executionItemId, cancellationToken).ConfigureAwait(false);
        if (!itemResult.IsSuccess)
        {
            return GenericResult.Failure(
                OperationsLog.ExecutionItemNotFound(_logger, executionItemId));
        }

        var item = itemResult.Value!;
        item.ResultCode = resultCode;
        item.ResultMessage = resultMessage;

        var updateResult = await PersistItemUpdate(item, executionItemId, cancellationToken).ConfigureAwait(false);
        if (!updateResult.IsSuccess)
        {
            return updateResult;
        }

        var transitionResult = await TransitionState(
            executionItemId, targetState, resultMessage, null,
            cancellationToken).ConfigureAwait(false);

        if (!transitionResult.IsSuccess)
        {
            return transitionResult;
        }

        if (success)
        {
            OperationsLog.ExecutionItemCompletedSuccess(_logger, executionItemId);
        }
        else
        {
            OperationsLog.ExecutionItemCompletedFailure(_logger, executionItemId, resultCode);
        }

        await EmitTerminalNotifications(item, targetState, success, cancellationToken).ConfigureAwait(false);

        return GenericResult.Success();
    }

    /// <inheritdoc />
    public async Task<IGenericResult<IExecutionItem>> GetItem(
        Guid executionItemId,
        CancellationToken cancellationToken = default)
    {
        // Reported here rather than refused at registration: a host that never tracks an
        // execution is not misconfigured for having no operational store.
        if (!StoreIsNamed)
            return GenericResult<IExecutionItem>.Failure(OperationsLog.OperationalConnectionNotSet(_logger));

        var result = await GetItemInternal(executionItemId, cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            return GenericResult<IExecutionItem>.Failure(
                OperationsLog.ExecutionItemNotFound(_logger, executionItemId));
        }

        return GenericResult<IExecutionItem>.Success(new ExecutionItemRecord(result.Value!));
    }

    /// <inheritdoc />
    public async Task<IGenericResult<IReadOnlyList<IExecutionEvent>>> GetEvents(
        Guid executionItemId,
        CancellationToken cancellationToken = default)
    {
        // Reported here rather than refused at registration: a host that never tracks an
        // execution is not misconfigured for having no operational store.
        if (!StoreIsNamed)
            return GenericResult<IReadOnlyList<IExecutionEvent>>.Failure(OperationsLog.OperationalConnectionNotSet(_logger));

        // Why the item is read first: events carry no tenant of their own; they are the item's, so an
        // item the caller does not reach has no events for them -- the same answer as an unknown id.
        var itemResult = await GetItemInternal(executionItemId, cancellationToken).ConfigureAwait(false);
        if (!itemResult.IsSuccess)
        {
            return GenericResult<IReadOnlyList<IExecutionEvent>>.Success(new List<IExecutionEvent>());
        }

        var queryCommand = new QueryCommand<ExecutionEvent>
        {
            Filter = new FilterExpression
            {
                Root = new FilterCondition
                {
                    PropertyName = "ExecutionItemId",
                    Operator = FilterOperators.ByName("Equal"),
                    Value = executionItemId
                }
            },
            Ordering = new OrderingExpression
            {
                OrderedFields =
                [
                    new OrderedField
                    {
                        PropertyName = "SequenceNumber",
                        Direction = SortDirections.Ascending
                    }
                ]
            }
        };

        var result = await Gateway.Execute<IEnumerable<ExecutionEvent>>(
            queryCommand, TargetFor(ContainerNameExecutionEvent), cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            return result.ToNewResult<IReadOnlyList<IExecutionEvent>>();
        }

        var events = result.Value?.Select(e => (IExecutionEvent)new ExecutionEventRecord(e)).ToList()
            ?? new List<IExecutionEvent>();

        return GenericResult<IReadOnlyList<IExecutionEvent>>.Success(events);
    }

    /// <inheritdoc />
    public async Task<IGenericResult<IReadOnlyList<IExecutionItem>>> GetChildren(
        Guid parentId,
        CancellationToken cancellationToken = default)
    {
        // Reported here rather than refused at registration: a host that never tracks an
        // execution is not misconfigured for having no operational store.
        if (!StoreIsNamed)
            return GenericResult<IReadOnlyList<IExecutionItem>>.Failure(OperationsLog.OperationalConnectionNotSet(_logger));

        var scope = CallerScope();
        if (scope.ReachesNothing)
            return GenericResult<IReadOnlyList<IExecutionItem>>.Success(new List<IExecutionItem>());

        var queryCommand = new QueryCommand<ExecutionItem>
        {
            Filter = Scoped(new FilterCondition
            {
                PropertyName = "ParentExecutionItemId",
                Operator = FilterOperators.ByName("Equal"),
                Value = parentId
            }, scope),
            Ordering = new OrderingExpression
            {
                OrderedFields =
                [
                    new OrderedField
                    {
                        PropertyName = "CreatedAt",
                        Direction = SortDirections.Ascending
                    }
                ]
            }
        };

        var result = await Gateway.Execute<IEnumerable<ExecutionItem>>(
            queryCommand, TargetFor(ContainerNameExecutionItem), cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            return result.ToNewResult<IReadOnlyList<IExecutionItem>>();
        }

        var items = result.Value?.Select(i => (IExecutionItem)new ExecutionItemRecord(i)).ToList()
            ?? new List<IExecutionItem>();

        return GenericResult<IReadOnlyList<IExecutionItem>>.Success(items);
    }

    /// <inheritdoc />
    public async Task<IGenericResult<IReadOnlyList<IExecutionItem>>> GetItems(
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        // Reported here rather than refused at registration: a host that never tracks an
        // execution is not misconfigured for having no operational store.
        if (!StoreIsNamed)
            return GenericResult<IReadOnlyList<IExecutionItem>>.Failure(OperationsLog.OperationalConnectionNotSet(_logger));

        var scope = CallerScope();
        if (scope.ReachesNothing)
            return GenericResult<IReadOnlyList<IExecutionItem>>.Success(new List<IExecutionItem>());

        var queryCommand = new QueryCommand<ExecutionItem>
        {
            Filter = Scoped(new FilterCondition
            {
                PropertyName = "CorrelationId",
                Operator = FilterOperators.ByName("Equal"),
                Value = correlationId
            }, scope),
            Ordering = new OrderingExpression
            {
                OrderedFields =
                [
                    new OrderedField
                    {
                        PropertyName = "CreatedAt",
                        Direction = SortDirections.Ascending
                    }
                ]
            }
        };

        var result = await Gateway.Execute<IEnumerable<ExecutionItem>>(
            queryCommand, TargetFor(ContainerNameExecutionItem), cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            return result.ToNewResult<IReadOnlyList<IExecutionItem>>();
        }

        var items = result.Value?.Select(i => (IExecutionItem)new ExecutionItemRecord(i)).ToList()
            ?? new List<IExecutionItem>();

        if (items.Count == 0)
        {
            OperationsLog.CorrelationIdNotFound(_logger, correlationId);
        }
        else
        {
            OperationsLog.CorrelationIdFound(_logger, correlationId, items.Count);
        }

        return GenericResult<IReadOnlyList<IExecutionItem>>.Success(items);
    }

    /// <inheritdoc />
    public async Task<IGenericResult<IPagedResponse<IExecutionItem>>> ListExecutions(
        int page = 1,
        int pageSize = 50,
        IExecutionItemType? itemType = null,
        IExecutionStateType? state = null,
        string? correlationId = null,
        Guid? rootId = null,
        CancellationToken cancellationToken = default)
    {
        // Reported here rather than refused at registration: a host that never tracks an
        // execution is not misconfigured for having no operational store.
        if (!StoreIsNamed)
            return GenericResult<IPagedResponse<IExecutionItem>>.Failure(OperationsLog.OperationalConnectionNotSet(_logger));

        var scope = CallerScope();
        if (scope.ReachesNothing)
        {
            return GenericResult<IPagedResponse<IExecutionItem>>.Success(
                new PagedResponse<IExecutionItem>(new List<IExecutionItem>(), page, pageSize, 0));
        }

        var filter = BuildListFilter(itemType, state, correlationId, rootId, scope);

        var queryCommand = new QueryCommand<ExecutionItem>
        {
            Ordering = new OrderingExpression
            {
                OrderedFields =
                [
                    new OrderedField
                    {
                        PropertyName = "CreatedAt",
                        Direction = SortDirections.Descending
                    }
                ]
            },
            Paging = new PagingExpression
            {
                Skip = (page - 1) * pageSize,
                Take = pageSize
            },
            Filter = filter
        };

        var result = await Gateway.Execute<IEnumerable<ExecutionItem>>(
            queryCommand, TargetFor(ContainerNameExecutionItem), cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess)
            return result.ToNewResult<IPagedResponse<IExecutionItem>>();

        var items = result.Value?.Select(i => (IExecutionItem)new ExecutionItemRecord(i)).ToList()
            ?? new List<IExecutionItem>();

        var totalEstimate = items.Count < pageSize
            ? (long)((page - 1) * pageSize + items.Count)
            : (long)((page + 1) * pageSize);

        var paged = new PagedResponse<IExecutionItem>(items, page, pageSize, totalEstimate);
        return GenericResult<IPagedResponse<IExecutionItem>>.Success(paged);
    }

    private static FilterExpression? BuildListFilter(
        IExecutionItemType? itemType,
        IExecutionStateType? state,
        string? correlationId,
        Guid? rootId,
        ExecutionTenantScope scope)
    {
        var conditions = new List<FilterCondition>();

        if (TenantCondition(scope) is { } tenant)
            conditions.Add(tenant);

        if (itemType != null)
        {
            conditions.Add(new FilterCondition
            {
                PropertyName = "ItemType",
                Operator = FilterOperators.ByName("Equal"),
                Value = itemType.Name
            });
        }

        if (state != null)
        {
            conditions.Add(new FilterCondition
            {
                PropertyName = "State",
                Operator = FilterOperators.ByName("Equal"),
                Value = state.Name
            });
        }

        if (!string.IsNullOrEmpty(correlationId))
        {
            conditions.Add(new FilterCondition
            {
                PropertyName = "CorrelationId",
                Operator = FilterOperators.ByName("Equal"),
                Value = correlationId
            });
        }

        if (rootId is { } root)
        {
            conditions.Add(new FilterCondition
            {
                PropertyName = "RootExecutionItemId",
                Operator = FilterOperators.ByName("Equal"),
                Value = root
            });
        }

        if (conditions.Count == 0)
            return null;

        if (conditions.Count == 1)
            return new FilterExpression { Root = conditions[0] };

        return new FilterExpression
        {
            Root = new FilterGroup
            {
                Operator = LogicalOperator.And,
                Nodes = conditions.Cast<IFilterNode>().ToList()
            }
        };
    }

    private async Task<IGenericResult<(Guid RootId, Guid? ParentTenantId)>> ValidateParent(
        IExecutionItemType itemType,
        string name,
        Guid? parentId,
        CancellationToken cancellationToken)
    {
        if (!parentId.HasValue)
        {
            return GenericResult<(Guid, Guid?)>.Success((Guid.NewGuid(), null));
        }

        // Scoped like every other read: a parent the caller does not reach is a parent not found.
        var domainResult = await GetItemInternal(parentId.Value, cancellationToken).ConfigureAwait(false);
        if (!domainResult.IsSuccess)
        {
            return GenericResult<(Guid, Guid?)>.Failure(
                OperationsLog.ParentExecutionItemNotFound(_logger, parentId.Value, name));
        }

        var parentItem = domainResult.Value!;
        var parentType = ExecutionItemTypes.ByName(parentItem.ItemType);

        if (!parentType.CanContain(itemType))
        {
            return GenericResult<(Guid, Guid?)>.Failure(
                OperationsLog.InvalidContainment(_logger, parentType.Name, itemType.Name));
        }

        return GenericResult<(Guid, Guid?)>.Success((parentItem.RootExecutionItemId, parentItem.TenantId));
    }

    // The tenant a new item belongs to. A child is its parent's; a root states its own, and a caller
    // confined to one tenant may state only that one. Nothing is assumed when the tenant is missing.
    private IGenericResult<Guid> ResolveTenant(string name, Guid? tenantId, Guid? parentId, Guid? parentTenantId)
    {
        if (parentId is { } parent && parentTenantId is { } inherited)
        {
            return tenantId is { } stated && stated != inherited
                ? GenericResult<Guid>.Failure(OperationsLog.ExecutionTenantDiffersFromParent(_logger, name, stated, parent, inherited))
                : GenericResult<Guid>.Success(inherited);
        }

        if (tenantId is not { } rootTenant)
            return GenericResult<Guid>.Failure(OperationsLog.ExecutionTenantRequired(_logger, name));

        return CallerScope().Reaches(rootTenant)
            ? GenericResult<Guid>.Success(rootTenant)
            : GenericResult<Guid>.Failure(OperationsLog.ExecutionTenantNotCallers(_logger, name, rootTenant));
    }

    private ExecutionTenantScope CallerScope() => ExecutionTenantScope.For(_authContext.Current);

    // The tenant term every list read carries, or none when the caller reaches every tenant.
    private static FilterCondition? TenantCondition(ExecutionTenantScope scope)
        => scope.ReachesAll
            ? null
            : new FilterCondition
            {
                PropertyName = "TenantId",
                Operator = FilterOperators.ByName("Equal"),
                Value = scope.TenantId
            };

    private static FilterExpression Scoped(FilterCondition condition, ExecutionTenantScope scope)
    {
        if (TenantCondition(scope) is not { } tenant)
            return new FilterExpression { Root = condition };

        return new FilterExpression
        {
            Root = new FilterGroup
            {
                Operator = LogicalOperator.And,
                Nodes = [condition, tenant]
            }
        };
    }

    private async Task<IGenericResult> PersistNewItem(
        ExecutionItem poco,
        CancellationToken cancellationToken)
    {
        var insertResult = await Gateway.Execute<int>(
            new InsertCommand<ExecutionItem>(poco),
            TargetFor(ContainerNameExecutionItem),
            cancellationToken).ConfigureAwait(false);
        if (!insertResult.IsSuccess)
        {
            return GenericResult.Failure(
                OperationsLog.ExecutionItemPersistFailed(_logger, poco.Id, insertResult.CurrentMessage ?? "Unknown error"));
        }

        return GenericResult.Success();
    }

    private IGenericResult ValidateStateTransition(
        Guid executionItemId,
        IExecutionStateType currentState,
        IExecutionStateType newState)
    {
        if (currentState.IsTerminal)
        {
            return GenericResult.Failure(
                OperationsLog.ExecutionItemAlreadyCompleted(_logger, executionItemId, currentState.Name));
        }

        if (!currentState.CanTransitionTo(newState.Name))
        {
            return GenericResult.Failure(
                OperationsLog.InvalidStateTransition(_logger, executionItemId, currentState.Name, newState.Name));
        }

        return GenericResult.Success();
    }

    private static void ApplyTimingFields(ExecutionItem item, IExecutionStateType newState)
    {
        if (string.Equals(newState.Name, ExecutionStateTypes.Running.Name, StringComparison.Ordinal) && !item.StartedAt.HasValue)
        {
            item.StartedAt = DateTimeOffset.UtcNow;
        }

        if (newState.IsTerminal && !item.CompletedAt.HasValue)
        {
            item.CompletedAt = DateTimeOffset.UtcNow;
            if (item.StartedAt.HasValue)
            {
                item.DurationMs = (long)(item.CompletedAt.Value - item.StartedAt.Value).TotalMilliseconds;
            }
        }
    }

    private async Task<IGenericResult> PersistItemUpdate(
        ExecutionItem item,
        Guid executionItemId,
        CancellationToken cancellationToken)
    {
        var updateResult = await Gateway.Execute<int>(
            new UpdateCommand<ExecutionItem>(item)
            {
                Filter = new FilterExpression
                {
                    Root = new FilterCondition
                    {
                        PropertyName = "Id",
                        Operator = FilterOperators.ByName("Equal"),
                        Value = executionItemId
                    }
                }
            },
            TargetFor(ContainerNameExecutionItem),
            cancellationToken).ConfigureAwait(false);
        if (!updateResult.IsSuccess)
        {
            return GenericResult.Failure(
                OperationsLog.ExecutionItemPersistFailed(_logger, executionItemId, updateResult.CurrentMessage ?? "Unknown error"));
        }

        return GenericResult.Success();
    }

    private async Task<IGenericResult<ExecutionItem>> GetItemInternal(
        Guid executionItemId,
        CancellationToken cancellationToken)
    {
        var queryCommand = new QueryCommand<ExecutionItem>
        {
            Filter = new FilterExpression
            {
                Root = new FilterCondition
                {
                    PropertyName = "Id",
                    Operator = FilterOperators.ByName("Equal"),
                    Value = executionItemId
                }
            }
        };

        var result = await Gateway.Execute<IEnumerable<ExecutionItem>>(
            queryCommand, TargetFor(ContainerNameExecutionItem), cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            return result.ToNewResult<ExecutionItem>();
        }

        // An item of a tenant the caller does not reach answers exactly as one that does not exist, so no
        // read, transition, event or completion can touch another tenant's execution.
        var item = result.Value?.FirstOrDefault();
        if (item == null || !CallerScope().Reaches(item.TenantId))
        {
            return GenericResult<ExecutionItem>.Failure(
                OperationsLog.ExecutionItemNotFound(_logger, executionItemId));
        }

        return GenericResult<ExecutionItem>.Success(item);
    }

    private async Task<int> GetNextSequenceNumber(Guid executionItemId, CancellationToken cancellationToken)
    {
        var queryCommand = new QueryCommand<ExecutionEvent>
        {
            Filter = new FilterExpression
            {
                Root = new FilterCondition
                {
                    PropertyName = "ExecutionItemId",
                    Operator = FilterOperators.ByName("Equal"),
                    Value = executionItemId
                }
            },
            Ordering = new OrderingExpression
            {
                OrderedFields =
                [
                    new OrderedField
                    {
                        PropertyName = "SequenceNumber",
                        Direction = SortDirections.Descending
                    }
                ]
            },
            Paging = new PagingExpression { Skip = 0, Take = 1 }
        };

        var result = await Gateway.Execute<IEnumerable<ExecutionEvent>>(
            queryCommand, TargetFor(ContainerNameExecutionEvent), cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            return 1;
        }

        var lastEvent = result.Value?.FirstOrDefault();
        return (lastEvent?.SequenceNumber ?? 0) + 1;
    }

    private async Task EmitTerminalNotifications(
        ExecutionItem item,
        IExecutionStateType targetState,
        bool success,
        CancellationToken cancellationToken)
    {
        try
        {
            if (_notificationProvider is null || _notificationRuleProvider is null)
            {
                OperationsLog.NotificationsSkippedNotConfigured(_logger, item.Id);
                return;
            }

            if (item.ParentExecutionItemId.HasValue)
            {
                return;
            }

            var rulesResult = await _notificationRuleProvider.Get(cancellationToken).ConfigureAwait(false);
            if (!rulesResult.IsSuccess)
            {
                OperationsLog.NotificationRulesLoadFailed(_logger, item.Id);
                return;
            }

            var record = new ExecutionItemRecord(item);

            foreach (var rule in rulesResult.Value!)
            {
                if (!rule.IsEnabled)
                {
                    continue;
                }

                if (!NotificationRuleScope.Covers(rule, record.RootId, ParameterGuid(record, "PipelineId"), ParameterGuid(record, "ScheduleId")))
                {
                    continue;
                }

                var metadata = new Dictionary<string, object?>(StringComparer.Ordinal)
                {
                    ["ExecutionItemId"] = item.Id,
                    ["RootId"] = item.RootExecutionItemId,
                    ["State"] = targetState.Name,
                    ["Success"] = success,
                    ["ResultCode"] = item.ResultCode,
                    ["ResultMessage"] = item.ResultMessage
                };

                var delivered = await NotificationRuleDelivery.Deliver(
                    _notificationProvider,
                    rule,
                    new RuleNotification
                    {
                        RuleName = rule.Name,
                        Subject = $"[{rule.Severity}] {item.Name} {targetState.Name}",
                        Message = BuildNotificationMessage(rule, record, targetState),
                        Metadata = metadata,
                        CorrelationId = item.CorrelationId
                    },
                    _logger,
                    cancellationToken).ConfigureAwait(false);

                LogDelivery(item.Id, rule, delivered);
            }
        }
        catch (OperationCanceledException ex)
        {
            _ = ex;
        }
        catch (Exception ex)
        {
            OperationsLog.NotificationEmissionError(_logger, item.Id, ex);
        }
    }

    private void LogDelivery(Guid itemId, INotificationRuleImplementationConfiguration rule, IGenericResult delivered)
    {
        if (delivered.IsSuccess)
        {
            OperationsLog.NotificationSent(_logger, itemId, rule.Name, rule.NotificationServiceName);
        }
        else if (delivered.Code == NotificationResultCodes.ByName("ChannelNotFound"))
        {
            OperationsLog.NotificationChannelUnresolved(_logger, rule.Name, rule.NotificationServiceName);
        }
        else if (delivered.Code == NotificationResultCodes.ByName("RuleSeverityInvalid"))
        {
            OperationsLog.NotificationSeverityInvalid(_logger, rule.Name, rule.Severity);
        }
        else
        {
            OperationsLog.NotificationSendFailed(_logger, itemId, rule.Name, rule.NotificationServiceName);
        }
    }

    private static Guid? ParameterGuid(ExecutionItemRecord record, string parameterKey)
    {
        if (!record.Parameters.TryGetValue(parameterKey, out var paramObj) || paramObj is null)
        {
            return null;
        }

        return Guid.TryParse(paramObj.ToString(), out var parsed) ? parsed : null;
    }

    private static string BuildNotificationMessage(
        INotificationRuleImplementationConfiguration rule,
        ExecutionItemRecord record,
        IExecutionStateType targetState)
    {
        if (!string.IsNullOrEmpty(rule.Template))
        {
            return rule.Template
                .Replace("{Name}", record.Name, StringComparison.Ordinal)
                .Replace("{State}", targetState.Name, StringComparison.Ordinal)
                .Replace("{ResultCode}", record.ResultCode ?? string.Empty, StringComparison.Ordinal)
                .Replace("{ResultMessage}", record.ResultMessage ?? string.Empty, StringComparison.Ordinal)
                .Replace("{RootId}", record.RootId.ToString(), StringComparison.Ordinal);
        }

        var baseMessage = $"Execution '{record.Name}' {targetState.Name}.";
        if (!string.IsNullOrEmpty(record.ResultCode))
        {
            baseMessage += $" Code: {record.ResultCode}.";
        }

        if (!string.IsNullOrEmpty(record.ResultMessage))
        {
            baseMessage += $" {record.ResultMessage}";
        }

        return baseMessage;
    }

    private async Task<ExecutionEvent?> RecordEventInternal(
        Guid executionItemId,
        int sequenceNumber,
        string eventType,
        string? previousState,
        string? newState,
        string? message,
        IReadOnlyDictionary<string, object?>? data,
        string? actor,
        CancellationToken cancellationToken)
    {
        var eventPoco = ExecutionEventRecord.CreatePoco(
            executionItemId,
            sequenceNumber,
            eventType,
            previousState,
            newState,
            message,
            data,
            actor);

        var insertResult = await Gateway.Execute<int>(
            new InsertCommand<ExecutionEvent>(eventPoco),
            TargetFor(ContainerNameExecutionEvent),
            cancellationToken).ConfigureAwait(false);

        return insertResult.IsSuccess ? eventPoco : null;
    }
}
