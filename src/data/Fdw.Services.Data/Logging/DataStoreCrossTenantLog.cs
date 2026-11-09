using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Logging;

/// <summary>Messages of a data store write that may belong to another tenant (FDW-4459).</summary>
[MessageLoggingTypeCode("DATA")]
public static partial class DataStoreCrossTenantLog
{
    /// <summary>Logs that a data store write is made as the tenant the store belongs to.</summary>
    [MessageLogging(EventId = 11880, Level = LogLevel.Information, Message = "Data store {dataStoreId} is written as tenant {ownerTenantId}, which it belongs to, for an administrator of tenant {actorTenantId}")]
    public static partial IGenericMessage WrittenAsOwner(ILogger logger, Guid dataStoreId, Guid ownerTenantId, Guid actorTenantId);

    /// <summary>Logs that the tenant of a data store could not be read, so it was not written.</summary>
    [MessageLogging(EventId = 90001, Level = LogLevel.Error, Message = "The tenant of data store {dataStoreId} could not be read, so it was not written: {reason}", ClientMessage = "The tenant of data store {dataStoreId} could not be read, so it was not written")]
    public static partial IGenericMessage OwnerNotRead(ILogger logger, Guid dataStoreId, string reason);

    /// <summary>Logs that a data store has no current row the reader can see.</summary>
    [MessageLogging(EventId = 30001, Level = LogLevel.Error, Message = "Data store {dataStoreId} has no current row the reader can see, so it was not written", ClientMessage = "Data store {dataStoreId} has no current row the reader can see, so it was not written")]
    public static partial IGenericMessage StoreNotFound(ILogger logger, Guid dataStoreId);

    /// <summary>Logs that the store's row-level security refused the write of a data store. The number is in the Forbidden category (10).</summary>
    [MessageLogging(EventId = 103211, Level = LogLevel.Warning, Message = "Data store {dataStoreId} could not be written by tenant {actorTenantId}: the store refused the write",
        ClientMessage = "The data store refused the change from this tenant")]
    public static partial IGenericMessage WriteRefusedByStore(ILogger logger, Guid dataStoreId, Guid? actorTenantId);

    /// <summary>Logs that a data store write is about to ask the cross-tenant change policy.</summary>
    [MessageLogging(EventId = 11883, Level = LogLevel.Trace, Message = "Data store {dataStoreId} belongs to tenant {ownerTenantId}; asking whether the change is allowed")]
    public static partial IGenericMessage AskingPolicy(ILogger logger, Guid dataStoreId, Guid ownerTenantId);

    /// <summary>Logs that the host registered the data store domain but nothing registered the cross-tenant change policy its writes ask.</summary>
    [MessageLogging(EventId = 72831, Level = LogLevel.Critical,
        Message = "The data store domain is registered but nothing registered ICrossTenantChangePolicy, which it asks before a data store is written; register the Settings service type (Fdw.Services.Settings, SettingsServiceTypes), which registers it; the host does not start",
        ClientMessage = "The data store domain needs the Settings service type to be registered, and it is not")]
    public static partial IGenericMessage CrossTenantChangePolicyNotRegistered(ILogger logger);
}
