using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Multitenancy.Logging;

/// <summary>
/// Messages of reading a tenant and a person's membership from the configuration store, for a host that runs the single-tenant option (FDW-6528). Each reason a lookup
/// fails is its own message, so a run's refusal says whether the person is not a member or the read failed.
/// </summary>
[MessageLoggingTypeCode("TENANTLOOKUP")]
public static partial class TenantLookupLog
{
    /// <summary>Logs that no tenant has the Id.</summary>
    [MessageLogging(EventId = 30001, Level = LogLevel.Warning, Message = "Tenant {tenantId} does not exist, or is not visible to the caller", ClientMessage = "The tenant was not found")]
    public static partial IGenericMessage TenantNotFound(ILogger logger, Guid tenantId);

    /// <summary>Logs that no tenant has the slug.</summary>
    [MessageLogging(EventId = 30002, Level = LogLevel.Warning, Message = "No tenant has the slug '{slug}', or it is not visible to the caller", ClientMessage = "The tenant was not found")]
    public static partial IGenericMessage TenantSlugNotFound(ILogger logger, string slug);

    /// <summary>Logs that the person given to a tenant access question is not an Id.</summary>
    [MessageLogging(EventId = 30003, Level = LogLevel.Warning, Message = "The person '{userId}' is not an Id, so whether they may act in a tenant cannot be asked", ClientMessage = "The person is not valid")]
    public static partial IGenericMessage UserIdNotAnId(ILogger logger, string userId);

    /// <summary>Logs that the configuration store could not be reached to read a tenant or a membership.</summary>
    [MessageLogging(EventId = 60001, Level = LogLevel.Error, Message = "The tenants could not be read: the configuration store is not available: {reason}")]
    public static partial IGenericMessage StoreUnavailable(ILogger logger, string reason);

    /// <summary>Logs a question the single-tenant option does not answer from the tenant rows.</summary>
    [MessageLogging(EventId = 60002, Level = LogLevel.Error, Message = "The single-tenant option answers tenant facts from the configuration store and does not answer '{operation}': this host resolves no tenant per request and reads no organization access")]
    public static partial IGenericMessage OperationNotAnswered(ILogger logger, string operation);

    /// <summary>Logs a container the single-tenant option reads that the host's schema does not declare, so the host does not start.</summary>
    [MessageLogging(EventId = 60003, Level = LogLevel.Critical, Message = "This host runs the SingleTenant multitenancy option, which reads tenants from container '{container}' of path '{path}' on data store '{dataStore}', and its configurationSchema.json does not declare it, so the host does not start")]
    public static partial IGenericMessage ContainerNotDeclared(ILogger logger, string dataStore, string path, string container);

    /// <summary>Logs that reading the tenants failed.</summary>
    [MessageLogging(EventId = 90001, Level = LogLevel.Error, Message = "The tenants could not be read: {reason}")]
    public static partial IGenericMessage ReadFailed(ILogger logger, string reason);

    /// <summary>Logs that reading a person's memberships failed.</summary>
    [MessageLogging(EventId = 90002, Level = LogLevel.Error, Message = "The memberships of person '{userId}' could not be read: {reason}")]
    public static partial IGenericMessage MembershipReadFailed(ILogger logger, string userId, string reason);
}
