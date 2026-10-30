using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Logging;

/// <summary>
/// MessageLogging for the watcher that empties the result cache when a configuration database is reset.
/// </summary>
[MessageLoggingTypeCode("DATA")]
public static partial class DatabaseResetWatcherLog
{
    /// <summary>Logged when the watcher starts.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="interval">How often the databases are looked at.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(EventId = 10001, Level = LogLevel.Information,
        Message = "Watching the configuration databases for a reset, looking every {interval}")]
    public static partial IGenericMessage Started(ILogger logger, TimeSpan interval);

    /// <summary>Logged when the host has no result cache, so there is nothing a reset could leave stale.</summary>
    /// <param name="logger">The logger.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(EventId = 10001, Level = LogLevel.Information,
        Message = "This host keeps no result cache, so a database reset cannot leave a cached read stale; the configuration databases are not watched")]
    public static partial IGenericMessage NoCache(ILogger logger);

    /// <summary>Logged the first time a database is looked at.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="connection">The configuration connection.</param>
    /// <param name="generation">The generation it reports, or a note that it could not be reached.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(EventId = 10001, Level = LogLevel.Debug,
        Message = "First look at the database behind connection '{connection}': {generation}")]
    public static partial IGenericMessage FirstLook(ILogger logger, string connection, string generation);

    /// <summary>Logged once for a connection whose kind cannot tell which generation of its database it reaches.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="connection">The configuration connection.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(EventId = 10001, Level = LogLevel.Debug,
        Message = "Connection '{connection}' cannot tell which generation of its database it reaches, so it is not watched")]
    public static partial IGenericMessage CannotTell(ILogger logger, string connection);

    /// <summary>Logged when a database was dropped and created again and the result cache was emptied.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="connection">The configuration connection.</param>
    /// <param name="previous">The generation it had.</param>
    /// <param name="current">The generation it has now.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(EventId = 10001, Level = LogLevel.Information,
        Message = "The database behind connection '{connection}' was reset (generation '{previous}' became '{current}'); the result cache was emptied")]
    public static partial IGenericMessage Reset(ILogger logger, string connection, string previous, string current);

    /// <summary>Logged when a database could not be reached and the result cache was emptied.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="connection">The configuration connection.</param>
    /// <param name="reason">Why it could not be reached.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(EventId = 71005, Level = LogLevel.Warning,
        Message = "The database behind connection '{connection}' cannot be reached ({reason}); the result cache was emptied")]
    public static partial IGenericMessage Unreachable(ILogger logger, string connection, string reason);

    /// <summary>Logged when a database that could not be reached can be reached again and the result cache was emptied.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="connection">The configuration connection.</param>
    /// <param name="generation">The generation it reports now.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(EventId = 10001, Level = LogLevel.Information,
        Message = "The database behind connection '{connection}' can be reached again at generation '{generation}'; the result cache was emptied")]
    public static partial IGenericMessage Reachable(ILogger logger, string connection, string generation);

    /// <summary>Logged when asking a gateway for its generation threw.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="ex">The exception.</param>
    /// <param name="connection">The configuration connection.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(EventId = 71006, Level = LogLevel.Error,
        Message = "Asking connection '{connection}' for the generation of its database failed")]
    public static partial IGenericMessage AskFailed(ILogger logger, Exception ex, string connection);

    /// <summary>Logged when the setting that says how often to look is missing or not positive.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="settingName">The setting.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(EventId = 20988, Level = LogLevel.Error,
        Message = "The setting '{settingName}' that says how often the configuration databases are looked at for a reset is missing or not positive, so the host cannot start")]
    public static partial IGenericMessage IntervalSettingMissing(ILogger logger, string settingName);

    /// <summary>Logged when the host registers no settings, so it cannot say how often to look.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="settingName">The setting that would say it.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(EventId = 60001, Level = LogLevel.Warning,
        Message = "This host registers no settings, so the setting '{settingName}' cannot be read; the configuration databases are not watched for a reset")]
    public static partial IGenericMessage NoIntervalSource(ILogger logger, string settingName);

    /// <summary>Logged when the watcher stops.</summary>
    /// <param name="logger">The logger.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(EventId = 10001, Level = LogLevel.Information,
        Message = "Stopped watching the configuration databases for a reset")]
    public static partial IGenericMessage Stopped(ILogger logger);
}
