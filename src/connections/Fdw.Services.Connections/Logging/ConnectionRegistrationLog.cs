using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Connections.Logging;

/// <summary>Messages of what the connection domain needs another domain to have registered (FDW-4459).</summary>
[MessageLoggingTypeCode("CONNECTIONS")]
public static partial class ConnectionRegistrationLog
{
    /// <summary>The host registered the connection domain but nothing registered the cross-tenant change policy it asks before it enables a connection.</summary>
    /// <param name="logger">The logger.</param>
    /// <returns>The message.</returns>
    [MessageLogging(EventId = 60340, Level = LogLevel.Critical,
        Message = "The connection domain is registered but nothing registered ICrossTenantChangePolicy, which it asks before a connection is enabled; register the Settings service type (Fdw.Services.Settings, SettingsServiceTypes), which registers it; the host does not start",
        ClientMessage = "The connection domain needs the Settings service type to be registered, and it is not")]
    public static partial IGenericMessage CrossTenantChangePolicyNotRegistered(ILogger logger);
}
