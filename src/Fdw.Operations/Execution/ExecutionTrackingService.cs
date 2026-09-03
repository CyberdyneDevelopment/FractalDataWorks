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
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Notifications;
using Fdw.Services.Notifications.Abstractions;
using Fdw.Services.Notifications.Configuration;
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

    // Why resolved here rather than injected: the gateway is scoped and this is not, so holding one
    // would be a captive dependency. The provider is asked when a call is actually being made.
    private IDataGateway Gateway => _dataGateways.ByName("Main");
    private readonly ILogger _logger;
    private readonly string? _dataStoreName;
    private readonly INotificationServiceProvider? _notificationProvider;
    private readonly IServiceConfigurationProvider<NotificationRuleConfiguration>? _notificationRuleProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExecutionTrackingService"/> class.
    /// </summary>
    /// <param name="dataGateways">The data gateway for persistence.</param>
    /// <param name="loggerFactory">Logger factory.</param>
    /// <param name="dataStoreName">
    /// The store the tracker reads and writes, or null when the host has not named one. Null is
    /// accepted rather than rejected here because every host registers the Operations option as part
    /// of the platform sweep, including hosts that never track an execution; refusing construction
    /// would stop those hosts booting. Each operation reports it instead.
    /// </param>
    /// <param name="notificationProvider">Optional notification service provider. When null, notification emission is skipped.</param>
    /// <param name="notificationRuleProvider">Optional notification rule configuration provider. When null, notification emission is skipped.</param>
    public ExecutionTrackingService(
        IDataGatewayProvider dataGateways,
        ILoggerFactory loggerFactory,
        string? dataStoreName,
        INotificationServiceProvider? notificationProvider = null,
        IServiceConfigurationProvider<NotificationRuleConfiguration>? notificationRuleProvider = null)
    {
        _dataGateways = dataGateways ?? throw new ArgumentNullException(nameof(dataGateways));
        _logger = loggerFactory?.CreateLogger<ExecutionTrackingService>()
            ?? throw new ArgumentNullException(nameof(loggerFactory));
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

        var rootId = parentValidation.Value;

        var poco = ExecutionItemRecord.CreatePoco(itemType, name, parentId, rootId, correlationId, triggerSource, parameters);

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

        var queryCommand = new QueryCommand<ExecutionItem>
        {
            Filter = new FilterExpression
            {
                Root = new FilterCondition
                {
                    PropertyName = "ParentExecutionItemId",
                    Operator = FilterOperators.ByName("Equal"),
                    Value = parentId
                }
            },
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

        var queryCommand = new QueryCommand<ExecutionItem>
        {
            Filter = new FilterExpression
            {
                Root = new FilterCondition
                {
                    PropertyName = "CorrelationId",
                    Operator = FilterOperators.ByName("Equal"),
                    Value = correlationId
                }
            },
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
        CancellationToken cancellationToken = default)
    {
        // Reported here rather than refused at registration: a host that never tracks an
        // execution is not misconfigured for having no operational store.
        if (!StoreIsNamed)
            return GenericResult<IPagedResponse<IExecutionItem>>.Failure(OperationsLog.OperationalConnectionNotSet(_logger));

        var filter = BuildListFilter(itemType, state, correlationId);

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
        string? correlationId)
    {
        var conditions = new List<FilterCondition>();

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

    private async Task<IGenericResult<Guid>> ValidateParent(
        IExecutionItemType itemType,
        string name,
        Guid? parentId,
        CancellationToken cancellationToken)
    {
        if (!parentId.HasValue)
        {
            return GenericResult<Guid>.Success(Guid.NewGuid());
        }

        var domainResult = await GetItemInternal(parentId.Value, cancellationToken).ConfigureAwait(false);
        if (!domainResult.IsSuccess)
        {
            return GenericResult<Guid>.Failure(
                OperationsLog.ParentExecutionItemNotFound(_logger, parentId.Value, name));
        }

        var parentItem = domainResult.Value!;
        var parentType = ExecutionItemTypes.ByName(parentItem.ItemType);

        if (!parentType.CanContain(itemType))
        {
            return GenericResult<Guid>.Failure(
                OperationsLog.InvalidContainment(_logger, parentType.Name, itemType.Name));
        }

        return GenericResult<Guid>.Success(parentItem.RootExecutionItemId);
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

        var item = result.Value?.FirstOrDefault();
        if (item == null)
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

                if (!RuleMatchesExecution(rule, record))
                {
                    continue;
                }

                var svcResult = await _notificationProvider.Get(rule.NotificationServiceName, cancellationToken).ConfigureAwait(false);
                if (!svcResult.IsSuccess || svcResult.Value is not INotificationService svc)
                {
                    OperationsLog.NotificationChannelUnresolved(_logger, rule.Name, rule.NotificationServiceName);
                    continue;
                }

                var priority = MapSeverityToPriority(rule.Severity);
                if (priority == NotificationPriorities.NotFound)
                {
                    OperationsLog.NotificationSeverityInvalid(_logger, rule.Name, rule.Severity);
                    continue;
                }

                var message = BuildNotificationMessage(rule, record, targetState);
                var metadata = new Dictionary<string, object?>(StringComparer.Ordinal)
                {
                    ["ExecutionItemId"] = item.Id,
                    ["RootId"] = item.RootExecutionItemId,
                    ["State"] = targetState.Name,
                    ["Success"] = success,
                    ["ResultCode"] = item.ResultCode,
                    ["ResultMessage"] = item.ResultMessage
                };

                var request = new NotificationRequest(
                    channelName: rule.NotificationServiceType,
                    recipients: [],
                    subject: $"[{rule.Severity}] {item.Name} {targetState.Name}",
                    message: message,
                    priority: priority,
                    metadata: metadata,
                    correlationId: item.CorrelationId);

                var sendResult = await svc.Send(request, cancellationToken).ConfigureAwait(false);
                if (sendResult.IsSuccess)
                {
                    OperationsLog.NotificationSent(_logger, item.Id, rule.Name, rule.NotificationServiceName);
                }
                else
                {
                    OperationsLog.NotificationSendFailed(_logger, item.Id, rule.Name, rule.NotificationServiceName);
                }
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

    /// <summary>
    /// Maps <see cref="NotificationRuleConfiguration.Severity"/> vocabulary to the priority
    /// TypeCollection using an explicit, case-insensitive total mapping. Returns
    /// <see cref="NotificationPriorities.NotFound"/> for unrecognised strings so the caller
    /// can fail loud rather than silently defaulting.
    /// </summary>
    /// <remarks>
    /// Why an explicit map instead of NotificationPriorities.ByName(severity): the Severity
    /// field uses log-level vocabulary ("Info", "Warning", "Error") while the priority
    /// TypeCollection uses delivery-urgency vocabulary ("Low", "Normal", "High", "Critical").
    /// A 1-to-1 name match would make the default severity "Info" always unmapped, breaking
    /// out-of-the-box behaviour. The deliberate map below gives "Info" → Normal so that a
    /// newly created rule emits without any extra configuration.
    /// </remarks>
    private static INotificationPriority MapSeverityToPriority(string severity)
    {
        if (string.Equals(severity, "Low", StringComparison.OrdinalIgnoreCase))
        {
            return NotificationPriorities.ByName("Low");
        }

        if (string.Equals(severity, "Info", StringComparison.OrdinalIgnoreCase)
            || string.Equals(severity, "Information", StringComparison.OrdinalIgnoreCase)
            || string.Equals(severity, "Normal", StringComparison.OrdinalIgnoreCase))
        {
            return NotificationPriorities.ByName("Normal");
        }

        if (string.Equals(severity, "Warning", StringComparison.OrdinalIgnoreCase)
            || string.Equals(severity, "Warn", StringComparison.OrdinalIgnoreCase)
            || string.Equals(severity, "High", StringComparison.OrdinalIgnoreCase))
        {
            return NotificationPriorities.ByName("High");
        }

        if (string.Equals(severity, "Error", StringComparison.OrdinalIgnoreCase)
            || string.Equals(severity, "Critical", StringComparison.OrdinalIgnoreCase)
            || string.Equals(severity, "Fatal", StringComparison.OrdinalIgnoreCase)
            || string.Equals(severity, "Severe", StringComparison.OrdinalIgnoreCase))
        {
            return NotificationPriorities.ByName("Critical");
        }

        return NotificationPriorities.NotFound;
    }

    private static bool RuleMatchesExecution(NotificationRuleConfiguration rule, ExecutionItemRecord record)
    {
        // Catch-all rule: no scope constraints.
        if (rule.PipelineId is null && rule.WorkflowId is null && rule.ScheduleId is null)
        {
            return true;
        }

        // Match by workflow (root execution) ID.
        if (rule.WorkflowId.HasValue && rule.WorkflowId.Value == record.RootId)
        {
            return true;
        }

        // Match by pipeline or schedule ID from execution parameters.
        return MatchesParameterGuid(record, "PipelineId", rule.PipelineId)
            || MatchesParameterGuid(record, "ScheduleId", rule.ScheduleId);
    }

    private static bool MatchesParameterGuid(ExecutionItemRecord record, string parameterKey, Guid? ruleValue)
    {
        if (!ruleValue.HasValue)
        {
            return false;
        }

        if (!record.Parameters.TryGetValue(parameterKey, out var paramObj) || paramObj is null)
        {
            return false;
        }

        return Guid.TryParse(paramObj.ToString(), out var parsed) && parsed == ruleValue.Value;
    }

    private static string BuildNotificationMessage(
        NotificationRuleConfiguration rule,
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
