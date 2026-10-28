using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Multitenancy.Logging;

/// <summary>
/// MessageLogging for the features switched on for tenants and organizations (FDW-1580).
/// EventId ranges: 12910-12911 (done), 32910 (not found, 404), 42910 (conflict, 409), 72910-72911 (store failures, 500), 102920 (organization not reachable, 403).
/// </summary>
[MessageLoggingTypeCode("MULTITENANCY")]
public static partial class FeatureEnablementLog
{
    /// <summary>Logs that a feature was switched on.</summary>
    [MessageLogging(EventId = 12910, Level = LogLevel.Information, Message = "Feature '{feature}' was switched on for {level} '{id}'")]
    public static partial IGenericMessage Enabled(ILogger logger, string feature, string level, Guid id);

    /// <summary>Logs that a feature was switched off.</summary>
    [MessageLogging(EventId = 12911, Level = LogLevel.Information, Message = "Feature '{feature}' was switched off for {level} '{id}'")]
    public static partial IGenericMessage Disabled(ILogger logger, string feature, string level, Guid id);

    /// <summary>Logs that the catalogue holds no such feature.</summary>
    [MessageLogging(EventId = 32910, Level = LogLevel.Warning, Message = "The feature catalogue holds no feature '{feature}'")]
    public static partial IGenericMessage FeatureNotFound(ILogger logger, string feature);

    /// <summary>Logs that a feature cannot be switched on for an organization while the tenant does not have it.</summary>
    [MessageLogging(EventId = 42910, Level = LogLevel.Warning, Message = "Feature '{feature}' was refused for organization '{organizationId}': the platform has not switched it on for tenant '{tenantId}'")]
    public static partial IGenericMessage NotEnabledForTenant(ILogger logger, string feature, Guid organizationId, Guid tenantId);

    /// <summary>Logs that a read from the store failed.</summary>
    [MessageLogging(EventId = 72910, Level = LogLevel.Error, Message = "{what} could not be read: {reason}")]
    public static partial IGenericMessage ReadFailed(ILogger logger, string what, string? reason);

    /// <summary>Logs that a write to the store failed.</summary>
    [MessageLogging(EventId = 72911, Level = LogLevel.Error, Message = "{what} could not be written: {reason}")]
    public static partial IGenericMessage WriteFailed(ILogger logger, string what, string? reason);
}
