using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Authentication.Logging;

/// <summary>
/// MessageLogging for re-issuing a caller's token for another of their tenants.
/// </summary>
[MessageLoggingTypeCode("AUTHENTICATION")]
internal static partial class TenantSwitchLog
{
    /// <summary>The step ran before the host finished initializing it.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="step">The step name.</param>
    [MessageLogging(EventId = 91250, Level = LogLevel.Error,
        Message = "Step '{step}' ran before its host initialized it; refusing to switch tenant")]
    internal static partial IGenericMessage NotInitialized(ILogger logger, string step);

    /// <summary>The request carried no authenticated caller.</summary>
    /// <param name="logger">The logger.</param>
    [MessageLogging(EventId = 51250, Level = LogLevel.Warning,
        Message = "A tenant switch was asked for by a request with no authenticated caller")]
    internal static partial IGenericMessage NoCaller(ILogger logger);

    /// <summary>The request named no tenant to switch to.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="callerId">Who asked.</param>
    [MessageLogging(EventId = 21250, Level = LogLevel.Warning,
        Message = "Principal {callerId} asked to switch tenant without naming one")]
    internal static partial IGenericMessage NoTenant(ILogger logger, Guid callerId);

    /// <summary>A caller is being re-issued for a tenant.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="callerId">Who asked.</param>
    /// <param name="tenantId">The tenant they asked to act in.</param>
    [MessageLogging(EventId = 11250, Level = LogLevel.Information,
        Message = "Principal {callerId} is switching to tenant {tenantId}")]
    internal static partial IGenericMessage Switching(ILogger logger, Guid callerId, Guid tenantId);
}
