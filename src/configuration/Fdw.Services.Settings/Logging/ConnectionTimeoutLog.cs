using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Settings.Logging;

/// <summary>
/// MessageLogging for the connect timeout of a connection that states none (FDW-3580).
/// EventId range: 62040-62049.
/// </summary>
[MessageLoggingTypeCode("SETTINGS")]
public static partial class ConnectionTimeoutLog
{
    // Why Error: the connection is not built, so nothing that needs it can run.
    /// <summary>Logs a connection that states no connect timeout when the instance default is not set to a positive number.</summary>
    [MessageLogging(EventId = 62040, Level = LogLevel.Error,
        Message = "The {ownerKind} '{ownerName}' states no connect timeout and the setting '{setting}' is not set to a positive number, so it is not built",
        ClientMessage = "The {ownerKind} '{ownerName}' states no connect timeout, and the setting '{setting}' that gives the default is not set to a positive number")]
    public static partial IGenericMessage NotStated(ILogger logger, string ownerKind, string ownerName, string setting);

    /// <summary>Logs a connection that states no connect timeout in a host that has no settings to read the default from.</summary>
    [MessageLogging(EventId = 62041, Level = LogLevel.Error,
        Message = "The {ownerKind} '{ownerName}' states no connect timeout and this host registers no settings to read '{setting}' from, so it is not built",
        ClientMessage = "The {ownerKind} '{ownerName}' states no connect timeout, and this host has no settings to read '{setting}' from")]
    public static partial IGenericMessage NoSettings(ILogger logger, string ownerKind, string ownerName, string setting);

    /// <summary>Logs a connection that states a connect timeout that is not a positive number of seconds.</summary>
    [MessageLogging(EventId = 62042, Level = LogLevel.Error,
        Message = "The {ownerKind} '{ownerName}' states a connect timeout of {seconds} seconds, which is not positive, so it is not built",
        ClientMessage = "The {ownerKind} '{ownerName}' states a connect timeout of {seconds} seconds; it must be greater than 0")]
    public static partial IGenericMessage NotPositive(ILogger logger, string ownerKind, string ownerName, int seconds);

    /// <summary>Logs a connection that states no connect timeout when the settings that would give it one are read through that very connection.</summary>
    [MessageLogging(EventId = 62043, Level = LogLevel.Error,
        Message = "The {ownerKind} '{ownerName}' states no connect timeout, and the setting '{setting}' cannot be read yet because the settings are read through the {ownerKind} being built; state a connect timeout in its configuration",
        ClientMessage = "The {ownerKind} '{ownerName}' states no connect timeout, and the settings that would give one are read through it; state a connect timeout in its configuration")]
    public static partial IGenericMessage ReadThroughItself(ILogger logger, string ownerKind, string ownerName, string setting);
}
