using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Logging;

/// <summary>
/// MessageLogging for <c>ConfigurationGatewayProvider</c>.
/// </summary>
[MessageLoggingTypeCode("DATA")]
public static partial class ConfigurationGatewayProviderLog
{
    /// <summary>A gateway was registered for a connection.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="connectionName">The connection the gateway operates on.</param>
    /// <param name="gatewayType">The gateway's type name.</param>
    [MessageLogging(EventId = 11014, Level = LogLevel.Debug,
        Message = "ConfigurationGatewayProvider: registered '{gatewayType}' for connection '{connectionName}'")]
    public static partial IGenericMessage GatewayRegistered(ILogger logger, string connectionName, string gatewayType);

    /// <summary>A caller asked for a gateway without naming a connection.</summary>
    /// <param name="logger">The logger.</param>
    [MessageLogging(EventId = 61020, Level = LogLevel.Error,
        Message = "ConfigurationGatewayProvider: no connection name was supplied")]
    public static partial IGenericMessage ConnectionNameMissing(ILogger logger);

    /// <summary>A caller registered a null gateway.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="connectionName">The connection the gateway would have served.</param>
    [MessageLogging(EventId = 61021, Level = LogLevel.Error,
        Message = "ConfigurationGatewayProvider: the gateway supplied for connection '{connectionName}' was null")]
    public static partial IGenericMessage GatewayNull(ILogger logger, string connectionName);
}
