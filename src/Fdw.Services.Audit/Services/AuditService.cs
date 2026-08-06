using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Audit.Abstractions;
using Fdw.Services.Audit.Logging;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Audit.Services;

/// <summary>
/// Implementation of <see cref="IAuditService"/> using IConfigurationGateway for persistence.
/// </summary>
// Why: audit.ConfigurationAudit lives in ConfigurationDb, which is reached via IConfigurationGateway.
// IConfigurationGateway has its own connection built from configurationSchema.json and does not depend
// on runtime IDataConnectionProvider — so it works even before connection rows are loaded from ConfigurationDb.
[ExcludeFromCodeCoverage]
public sealed class AuditService : IAuditService
{
    private readonly IConfigurationGateway _gateway;
    private readonly ILogger<AuditService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuditService"/> class.
    /// </summary>
    public AuditService(
        IConfigurationGateway gateway,
        ILogger<AuditService>? logger)
    {
        _gateway = gateway;
        _logger = logger ?? NullLogger<AuditService>.Instance;
    }

    /// <inheritdoc />
    public async Task<IGenericResult> RecordCreate(
        string entityType,
        string entityId,
        string afterState,
        AuditContext context,
        CancellationToken cancellationToken = default)
    {
        var record = new AuditInsertRecord
        {
            Id = Guid.NewGuid(),
            EntityType = entityType,
            EntityId = entityId,
            Action = "Create",
            BeforeJson = null,
            AfterJson = afterState,
            ChangedFields = null,
            UserId = context.UserId,
            UserName = context.UserName,
            IpAddress = context.IpAddress,
            UserAgent = context.UserAgent,
            Timestamp = DateTimeOffset.UtcNow,
            CorrelationId = context.CorrelationId
        };

        var result = await InsertAuditRecord(record, cancellationToken).ConfigureAwait(false);

        if (result.IsSuccess)
        {
            AuditLog.ConfigurationCreated(_logger, entityType, entityId, context.UserId);
            AuditLog.AuditRecordCreated(_logger, entityType, entityId);
        }
        else
        {
            AuditLog.AuditRecordCreateFailed(_logger, entityType, entityId);
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<IGenericResult> RecordUpdate(
        string entityType,
        string entityId,
        string beforeState,
        string afterState,
        string? changedFields,
        AuditContext context,
        CancellationToken cancellationToken = default)
    {
        var record = new AuditInsertRecord
        {
            Id = Guid.NewGuid(),
            EntityType = entityType,
            EntityId = entityId,
            Action = "Update",
            BeforeJson = beforeState,
            AfterJson = afterState,
            ChangedFields = changedFields,
            UserId = context.UserId,
            UserName = context.UserName,
            IpAddress = context.IpAddress,
            UserAgent = context.UserAgent,
            Timestamp = DateTimeOffset.UtcNow,
            CorrelationId = context.CorrelationId
        };

        var result = await InsertAuditRecord(record, cancellationToken).ConfigureAwait(false);

        if (result.IsSuccess)
        {
            AuditLog.ConfigurationUpdated(_logger, entityType, entityId, context.UserId);
            if (!string.IsNullOrEmpty(changedFields))
            {
                AuditLog.FieldsChanged(_logger, entityType, entityId, changedFields);
            }
        }
        else
        {
            AuditLog.AuditRecordCreateFailed(_logger, entityType, entityId);
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<IGenericResult> RecordDelete(
        string entityType,
        string entityId,
        string beforeState,
        AuditContext context,
        CancellationToken cancellationToken = default)
    {
        var record = new AuditInsertRecord
        {
            Id = Guid.NewGuid(),
            EntityType = entityType,
            EntityId = entityId,
            Action = "Delete",
            BeforeJson = beforeState,
            AfterJson = null,
            ChangedFields = null,
            UserId = context.UserId,
            UserName = context.UserName,
            IpAddress = context.IpAddress,
            UserAgent = context.UserAgent,
            Timestamp = DateTimeOffset.UtcNow,
            CorrelationId = context.CorrelationId
        };

        var result = await InsertAuditRecord(record, cancellationToken).ConfigureAwait(false);

        if (result.IsSuccess)
        {
            AuditLog.ConfigurationDeleted(_logger, entityType, entityId, context.UserId);
        }
        else
        {
            AuditLog.AuditRecordCreateFailed(_logger, entityType, entityId);
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<IGenericResult<AuditRecord[]>> GetAuditTrail(
        string entityType,
        string entityId,
        CancellationToken cancellationToken = default)
    {
        AuditLog.AuditTrailQueried(_logger, entityType, entityId);

        var filter = new FilterExpression
        {
            Root = new FilterGroup
            {
                Operator = LogicalOperator.And,
                Nodes =
                [
                    new FilterCondition
                    {
                        PropertyName = "EntityType",
                        Operator = FilterOperators.ByName("Equal"),
                        Value = entityType
                    },
                    new FilterCondition
                    {
                        PropertyName = "EntityId",
                        Operator = FilterOperators.ByName("Equal"),
                        Value = entityId
                    }
                ]
            }
        };

        var command = new QueryCommand<AuditQueryRecord>
        {
            // Why: Addressing lives in DataStoreTarget — audit.ConfigurationAudit is in
            // ConfigurationDb (DataStore) under the "audit" path (schema).
            Filter = filter,
            Ordering = new OrderingExpression
            {
                OrderedFields =
                [
                    new OrderedField
                    {
                        PropertyName = "Timestamp",
                        Direction = SortDirections.ByName("Descending")
                    }
                ]
            }
        };

        var result = await _gateway.Execute<IEnumerable<AuditQueryRecord>>(
            command,
            new DataStoreTarget("ConfigurationDb", "audit", "ConfigurationAudit"),
            cancellationToken).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            return result.ToNewResult<AuditRecord[]>();
        }

        if (result.Value == null)
        {
            AuditLog.AuditRecordsRetrieved(_logger, 0, entityType);
            return GenericResult<AuditRecord[]>.Success([]);
        }

        var records = result.Value.Select(MapToAuditRecord).ToArray();

        AuditLog.AuditRecordsRetrieved(_logger, records.Length, entityType);
        return GenericResult<AuditRecord[]>.Success(records);
    }

    /// <inheritdoc />
    public async Task<IGenericResult<AuditRecord[]>> ListAuditRecords(
        string? entityType,
        string? entityId,
        string? action,
        string? userId,
        int limit,
        CancellationToken cancellationToken = default)
    {
        AuditLog.AuditRecordsListed(_logger, entityType, action, limit);

        var conditions = new List<IFilterNode>();

        if (!string.IsNullOrWhiteSpace(entityType))
        {
            conditions.Add(new FilterCondition
            {
                PropertyName = "EntityType",
                Operator = FilterOperators.ByName("Equal"),
                Value = entityType
            });
        }

        if (!string.IsNullOrWhiteSpace(entityId))
        {
            conditions.Add(new FilterCondition
            {
                PropertyName = "EntityId",
                Operator = FilterOperators.ByName("Equal"),
                Value = entityId
            });
        }

        if (!string.IsNullOrWhiteSpace(action))
        {
            conditions.Add(new FilterCondition
            {
                PropertyName = "Action",
                Operator = FilterOperators.ByName("Equal"),
                Value = action
            });
        }

        if (!string.IsNullOrWhiteSpace(userId))
        {
            conditions.Add(new FilterCondition
            {
                PropertyName = "UserId",
                Operator = FilterOperators.ByName("Equal"),
                Value = userId
            });
        }

        FilterExpression? filter = conditions.Count switch
        {
            0 => null,
            1 => new FilterExpression { Root = conditions[0] },
            _ => new FilterExpression
            {
                Root = new FilterGroup
                {
                    Operator = LogicalOperator.And,
                    Nodes = conditions
                }
            }
        };

        var command = new QueryCommand<AuditQueryRecord>
        {
            // Why: Addressing lives in DataStoreTarget — audit.ConfigurationAudit is in
            // ConfigurationDb (DataStore) under the "audit" path (schema).
            Filter = filter,
            Ordering = new OrderingExpression
            {
                OrderedFields =
                [
                    new OrderedField
                    {
                        PropertyName = "Timestamp",
                        Direction = SortDirections.ByName("Descending")
                    }
                ]
            }
        };

        var result = await _gateway.Execute<IEnumerable<AuditQueryRecord>>(
            command,
            new DataStoreTarget("ConfigurationDb", "audit", "ConfigurationAudit"),
            cancellationToken).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            AuditLog.AuditRecordsListFailed(_logger);
            return result.ToNewResult<AuditRecord[]>();
        }

        var records = (result.Value ?? Enumerable.Empty<AuditQueryRecord>())
            .Take(limit)
            .Select(MapToAuditRecord)
            .ToArray();

        AuditLog.AuditRecordsRetrieved(_logger, records.Length, entityType);
        return GenericResult<AuditRecord[]>.Success(records);
    }

    private static AuditRecord MapToAuditRecord(AuditQueryRecord r) =>
        new(r.Id, r.EntityType, r.EntityId, r.Action,
            r.BeforeJson, r.AfterJson, r.ChangedFields,
            r.UserId, r.UserName, r.IpAddress,
            r.Timestamp, r.CorrelationId);

    private async Task<IGenericResult> InsertAuditRecord(AuditInsertRecord record, CancellationToken cancellationToken)
    {
        // Why: Addressing lives in DataStoreTarget — audit.ConfigurationAudit is in
        // ConfigurationDb (DataStore) under the "audit" path (schema).
        var command = new InsertCommand<AuditInsertRecord>(record);
        var result = await _gateway.Execute<int>(
            command,
            new DataStoreTarget("ConfigurationDb", "audit", "ConfigurationAudit"),
            cancellationToken).ConfigureAwait(false);
        return result.IsSuccess
            ? GenericResult.Success()
            : result.ToNewResult<int>();
    }

    private sealed record AuditInsertRecord
    {
        public Guid Id { get; init; }
        public required string EntityType { get; init; }
        public required string EntityId { get; init; }
        public required string Action { get; init; }
        public string? BeforeJson { get; init; }
        public string? AfterJson { get; init; }
        public string? ChangedFields { get; init; }
        public required string UserId { get; init; }
        public string? UserName { get; init; }
        public string? IpAddress { get; init; }
        public string? UserAgent { get; init; }
        public DateTimeOffset Timestamp { get; init; }
        public Guid? CorrelationId { get; init; }
    }

    private sealed record AuditQueryRecord
    {
        public Guid Id { get; init; }
        public string EntityType { get; init; } = string.Empty;
        public string EntityId { get; init; } = string.Empty;
        public string Action { get; init; } = string.Empty;
        public string? BeforeJson { get; init; }
        public string? AfterJson { get; init; }
        public string? ChangedFields { get; init; }
        public string UserId { get; init; } = string.Empty;
        public string? UserName { get; init; }
        public string? IpAddress { get; init; }
        public DateTimeOffset Timestamp { get; init; }
        public Guid? CorrelationId { get; init; }
    }
}
