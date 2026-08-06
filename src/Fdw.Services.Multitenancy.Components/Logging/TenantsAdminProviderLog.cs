using System;
using Microsoft.Extensions.Logging;
using Fdw.Messages;
using Fdw.MessageLogging;

namespace Fdw.Services.Multitenancy.Components.Logging;

/// <summary>
/// MessageLogging for TenantsAdminProvider headless component.
/// EventId range: 4274-4283
/// </summary>
[MessageLoggingTypeCode("COMPONENTS10")]
public static partial class TenantsAdminProviderLog
{
    [MessageLogging(EventId = 11000, Level = LogLevel.Trace, Message = "TenantsAdminProvider: Loading tenants list")]
    public static partial IGenericMessage LoadingTenants(ILogger logger);

    [MessageLogging(EventId = 11001, Level = LogLevel.Information, Message = "TenantsAdminProvider: Loaded {count} tenants")]
    public static partial IGenericMessage LoadedTenants(ILogger logger, int count);

    // Why (FDW-583): a caught exception reporting an operation that could not complete — Error, not Warning.
    [MessageLogging(EventId = 91000, Level = LogLevel.Error, Message = "TenantsAdminProvider: Failed to load tenants")]
    public static partial IGenericMessage LoadTenantsFailed(ILogger logger, Exception exception);

    [MessageLogging(EventId = 11002, Level = LogLevel.Trace, Message = "TenantsAdminProvider: Creating tenant '{name}'")]
    public static partial IGenericMessage CreatingTenant(ILogger logger, string name);

    [MessageLogging(EventId = 11003, Level = LogLevel.Information, Message = "TenantsAdminProvider: Tenant '{name}' created")]
    public static partial IGenericMessage TenantCreated(ILogger logger, string name);

    [MessageLogging(EventId = 91001, Level = LogLevel.Error, Message = "TenantsAdminProvider: Failed to create tenant '{name}'")]
    public static partial IGenericMessage CreateTenantFailed(ILogger logger, string name);

    [MessageLogging(EventId = 91002, Level = LogLevel.Error, Message = "TenantsAdminProvider: Failed to create tenant '{name}'")]
    public static partial IGenericMessage CreateTenantException(ILogger logger, Exception exception, string name);

    [MessageLogging(EventId = 11004, Level = LogLevel.Trace, Message = "TenantsAdminProvider: Updating tenant {tenantId}")]
    public static partial IGenericMessage UpdatingTenant(ILogger logger, string tenantId);

    [MessageLogging(EventId = 11005, Level = LogLevel.Information, Message = "TenantsAdminProvider: Tenant {tenantId} updated")]
    public static partial IGenericMessage TenantUpdated(ILogger logger, string tenantId);

    [MessageLogging(EventId = 91003, Level = LogLevel.Error, Message = "TenantsAdminProvider: Failed to update tenant {tenantId}")]
    public static partial IGenericMessage UpdateTenantFailed(ILogger logger, string tenantId);

    [MessageLogging(EventId = 91004, Level = LogLevel.Error, Message = "TenantsAdminProvider: Failed to update tenant {tenantId}")]
    public static partial IGenericMessage UpdateTenantException(ILogger logger, Exception exception, string tenantId);
}
