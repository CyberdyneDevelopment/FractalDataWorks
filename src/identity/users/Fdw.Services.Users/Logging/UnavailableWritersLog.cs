using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Users.Logging;

/// <summary>
/// MessageLogging for the refusing tenant and organization writers (FDW-4180). EventId range: 72741 (a tenant was asked to change on a host
/// that does not run the tenant service), 72742 (an organization was).
/// </summary>
[MessageLoggingTypeCode("USERS")]
public static partial class UnavailableWritersLog
{
    /// <summary>Logs that a tenant was asked to change on a host that does not run the tenant service.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="operation">What was asked: made, changed, deleted or marked deleted.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 72741, Level = LogLevel.Error, Message = "A tenant could not be {operation}: this host does not run the tenant service, so tenants cannot be written here")]
    public static partial IGenericMessage TenantWriterUnavailable(ILogger logger, string operation);

    /// <summary>Logs that an organization was asked to change on a host that does not run the tenant service.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="operation">What was asked: made, changed or deleted.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 72742, Level = LogLevel.Error, Message = "An organization could not be {operation}: this host does not run the tenant service, so organizations cannot be written here")]
    public static partial IGenericMessage OrganizationWriterUnavailable(ILogger logger, string operation);
}
