using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Multitenancy.Logging;

/// <summary>Messages of reading an organization from the configuration store (FDW-4573). Each reason a lookup fails is its own message.</summary>
[MessageLoggingTypeCode("ORGLOOKUP")]
public static partial class OrganizationLookupLog
{
    /// <summary>Logs that no current organization has the Id.</summary>
    [MessageLogging(EventId = 30001, Level = LogLevel.Warning, Message = "Organization {organizationId} does not exist, or is not visible to the caller", ClientMessage = "The organization was not found")]
    public static partial IGenericMessage OrganizationNotFound(ILogger logger, Guid organizationId);

    /// <summary>Logs that a tenant has no default organization.</summary>
    [MessageLogging(EventId = 30002, Level = LogLevel.Warning, Message = "Tenant {tenantId} has no default organization the caller can see", ClientMessage = "The tenant has no default organization")]
    public static partial IGenericMessage NoDefaultOrganization(ILogger logger, Guid tenantId);

    /// <summary>Logs that the configuration store could not be reached to read an organization.</summary>
    [MessageLogging(EventId = 60001, Level = LogLevel.Error, Message = "The organizations could not be read: the configuration store is not available: {reason}")]
    public static partial IGenericMessage StoreUnavailable(ILogger logger, string reason);

    /// <summary>Logs that reading the organizations failed.</summary>
    [MessageLogging(EventId = 90001, Level = LogLevel.Error, Message = "The organizations could not be read: {reason}")]
    public static partial IGenericMessage ReadFailed(ILogger logger, string reason);
}
