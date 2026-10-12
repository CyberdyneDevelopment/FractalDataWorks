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

    [MessageLogging(EventId = 11022, Level = LogLevel.Trace,
        Message = "Reading all org access grants for userId={userId}")]
    public static partial IGenericMessage GetByUserTrace(ILogger logger, System.Guid userId);

    [MessageLogging(EventId = 11023, Level = LogLevel.Debug,
        Message = "All org access grants loaded for userId={userId}: {count} grants")]
    public static partial IGenericMessage GetByUserLoaded(ILogger logger, System.Guid userId, int count);

    [MessageLogging(EventId = 71011, Level = LogLevel.Error,
        Message = "Failed to read all org access grants for userId={userId}")]
    public static partial IGenericMessage GetByUserFailed(ILogger logger, System.Guid userId, System.Exception ex);
}
