using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Authorization.Logging;

/// <summary>
/// MessageLogging for TenantOrgAccessConfigurationProvider operations.
/// EventId range: 3141-3150
/// </summary>
[MessageLoggingTypeCode("AUTHORIZATION")]
public static partial class TenantOrgAccessConfigurationProviderLog
{
    [MessageLogging(EventId = 11018, Level = LogLevel.Trace,
        Message = "Reading org access grants for userId={userId} tenantId={tenantId} orgId={orgId}")]
    public static partial IGenericMessage GetTrace(ILogger logger, System.Guid userId, System.Guid tenantId, System.Guid orgId);

    [MessageLogging(EventId = 11019, Level = LogLevel.Debug,
        Message = "Org access grants loaded for userId={userId} orgId={orgId}: {count} grants")]
    public static partial IGenericMessage GetLoaded(ILogger logger, System.Guid userId, System.Guid orgId, int count);

    [MessageLogging(EventId = 71009, Level = LogLevel.Error,
        Message = "Failed to read org access grants for userId={userId} orgId={orgId}")]
    public static partial IGenericMessage GetFailed(ILogger logger, System.Guid userId, System.Guid orgId, System.Exception ex);
}
