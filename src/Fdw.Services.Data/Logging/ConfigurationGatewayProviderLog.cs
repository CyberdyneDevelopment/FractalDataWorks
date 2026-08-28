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
    [MessageLogging(EventId = 61021, Level = LogLevel.Error,
        Message = "ConfigurationGatewayProvider: the gateway supplied for registration was null")]
    public static partial IGenericMessage GatewayNull(ILogger logger);

    /// <summary>A gateway was registered without naming the connection it opened.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="gatewayType">The gateway's type name.</param>
    [MessageLogging(EventId = 61023, Level = LogLevel.Error,
        Message = "ConfigurationGatewayProvider: '{gatewayType}' names no connection, so there is nothing to file it under")]
    public static partial IGenericMessage GatewayNamesNoConnection(ILogger logger, string gatewayType);

    /// <summary>A declared connection names no kind, so no factory can be chosen for it.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="connectionName">The connection as declared.</param>
    [MessageLogging(EventId = 61024, Level = LogLevel.Error,
        Message = "configurationSchema.json declares connection '{connectionName}' with no ServiceOptionType, so no connection factory can be chosen for it")]
    public static partial IGenericMessage ConnectionDeclaresNoKind(ILogger logger, string connectionName);

    /// <summary>A caller asked for a connection the schema does not declare.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="connectionName">The connection asked for.</param>
    [MessageLogging(EventId = 61027, Level = LogLevel.Error,
        Message = "configurationSchema.json declares no connection named '{connectionName}', so no configuration gateway can be built for it")]
    public static partial IGenericMessage ConnectionNotDeclared(ILogger logger, string connectionName);

    /// <summary>A declared connection names a kind no connection option registered.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="connectionName">The connection as declared.</param>
    /// <param name="serviceOptionType">The kind it named.</param>
    [MessageLogging(EventId = 61025, Level = LogLevel.Error,
        Message = "configurationSchema.json declares connection '{connectionName}' as kind '{serviceOptionType}', which no connection option registered")]
    public static partial IGenericMessage ConnectionKindNotRegistered(ILogger logger, string connectionName, string serviceOptionType);

    /// <summary>The factory a connection kind names is not in the container.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="connectionName">The connection as declared.</param>
    /// <param name="factoryType">The factory type its kind names.</param>
    [MessageLogging(EventId = 61026, Level = LogLevel.Error,
        Message = "connection '{connectionName}' names factory '{factoryType}', which is not registered — connections must register before configuration gateways")]
    public static partial IGenericMessage ConnectionFactoryUnavailable(ILogger logger, string connectionName, string factoryType);

}
