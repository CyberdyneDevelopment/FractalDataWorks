using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Logging;

/// <summary>
/// MessageLogging for the cache change tokens (FDW-4782): publishing a configuration change to the other hosts, and
/// a host applying the changes the others published.
/// </summary>
[MessageLoggingTypeCode("DATA")]
public static partial class CacheChangeLog
{
    /// <summary>Logged when the changed tags of a flush were published.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="count">How many tags were published.</param>
    /// <param name="tags">The tags.</param>
    /// <returns>The message.</returns>
    [MessageLogging(EventId = 11308, Level = LogLevel.Debug,
        Message = "Published {count} configuration change(s) to the other hosts: {tags}")]
    public static partial IGenericMessage Published(ILogger logger, int count, string tags);

    /// <summary>Logged when the platform configuration gateway the tokens are written through cannot be had.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="connection">The configuration connection.</param>
    /// <param name="tags">The tags that were not published.</param>
    /// <param name="reason">Why the gateway cannot be had.</param>
    /// <returns>The message.</returns>
    [MessageLogging(EventId = 71101, Level = LogLevel.Warning,
        Message = "Configuration changes ({tags}) were not published: the gateway onto '{connection}' cannot be had ({reason}); other hosts may show the old values until their cache entries expire")]
    public static partial IGenericMessage GatewayUnavailable(ILogger logger, string connection, string tags, string reason);

    /// <summary>Logged when updating a tag's token failed.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="tag">The tag.</param>
    /// <param name="reason">Why the update failed.</param>
    /// <returns>The message.</returns>
    [MessageLogging(EventId = 71102, Level = LogLevel.Warning,
        Message = "The change to '{tag}' was not published: updating its token failed ({reason}); other hosts may show the old values until their cache entries expire")]
    public static partial IGenericMessage UpdateFailed(ILogger logger, string tag, string reason);

    /// <summary>Logged when inserting a tag's first token failed and the update tried again after it failed too.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="tag">The tag.</param>
    /// <param name="insertReason">Why the insert failed.</param>
    /// <param name="retryReason">Why the update tried again failed.</param>
    /// <returns>The message.</returns>
    [MessageLogging(EventId = 71103, Level = LogLevel.Warning,
        Message = "The change to '{tag}' was not published: inserting its token failed ({insertReason}) and the update tried after it failed ({retryReason}); other hosts may show the old values until their cache entries expire")]
    public static partial IGenericMessage InsertAndRetryFailed(ILogger logger, string tag, string insertReason, string retryReason);

    /// <summary>
    /// Logged when inserting a tag's first token failed and the update tried again found no row either: the insert did not
    /// lose a race with another host, it failed for another reason.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="tag">The tag.</param>
    /// <param name="insertReason">Why the insert failed.</param>
    /// <returns>The message.</returns>
    [MessageLogging(EventId = 71104, Level = LogLevel.Warning,
        Message = "The change to '{tag}' was not published: inserting its token failed ({insertReason}) and no other host had inserted it; other hosts may show the old values until their cache entries expire")]
    public static partial IGenericMessage InsertFailedNoRow(ILogger logger, string tag, string insertReason);

    /// <summary>Logged when a flush of the changed tags threw.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="ex">The exception.</param>
    /// <param name="tags">The tags that were being published.</param>
    /// <returns>The message.</returns>
    [MessageLogging(EventId = 91101, Level = LogLevel.Error,
        Message = "Publishing configuration changes ({tags}) threw; other hosts may show the old values until their cache entries expire")]
    public static partial IGenericMessage FlushThrew(ILogger logger, Exception ex, string tags);

    /// <summary>Logged when the poller starts.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="interval">How often it looks.</param>
    /// <returns>The message.</returns>
    [MessageLogging(EventId = 11309, Level = LogLevel.Information,
        Message = "Looking at the configuration changes other hosts publish every {interval}")]
    public static partial IGenericMessage PollerStarted(ILogger logger, TimeSpan interval);

    /// <summary>Logged when the host keeps no result cache, so there is nothing to drop and the changes are not looked at.</summary>
    /// <param name="logger">The logger.</param>
    /// <returns>The message.</returns>
    [MessageLogging(EventId = 11310, Level = LogLevel.Information,
        Message = "This host keeps no result cache, so a configuration change in another host cannot leave a cached read stale; the published changes are not looked at")]
    public static partial IGenericMessage NoCache(ILogger logger);

    /// <summary>Logged on the first look, which only records the tokens.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="count">How many tags have a token.</param>
    /// <returns>The message.</returns>
    [MessageLogging(EventId = 11311, Level = LogLevel.Debug,
        Message = "First look at the published configuration changes: {count} tag(s) recorded, none dropped")]
    public static partial IGenericMessage FirstLook(ILogger logger, int count);

    /// <summary>Logged when a look found changes another host published, and dropped them from the cache.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="count">How many tags changed.</param>
    /// <param name="tags">The tags.</param>
    /// <returns>The message.</returns>
    [MessageLogging(EventId = 11312, Level = LogLevel.Information,
        Message = "{count} configuration change(s) published by another host were dropped from the result cache: {tags}")]
    public static partial IGenericMessage ChangesApplied(ILogger logger, int count, string tags);

    /// <summary>Logged when reading the tokens failed.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="reason">Why the read failed.</param>
    /// <returns>The message.</returns>
    [MessageLogging(EventId = 71105, Level = LogLevel.Warning,
        Message = "The published configuration changes could not be read ({reason}); the next look tries again")]
    public static partial IGenericMessage ReadFailed(ILogger logger, string reason);

    /// <summary>Logged when reading the tokens threw.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="ex">The exception.</param>
    /// <returns>The message.</returns>
    [MessageLogging(EventId = 91102, Level = LogLevel.Error,
        Message = "Reading the published configuration changes threw; the next look tries again")]
    public static partial IGenericMessage ReadThrew(ILogger logger, Exception ex);

    /// <summary>Logged when the poll interval setting states a number that is not positive, so a host with a cache cannot start.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="settingName">The setting.</param>
    /// <param name="seconds">The number it states.</param>
    /// <returns>The message.</returns>
    [MessageLogging(EventId = 61100, Level = LogLevel.Error,
        Message = "The setting '{settingName}' that says how often the published configuration changes are looked at states {seconds}, which is not positive, so a host that keeps a result cache cannot start")]
    public static partial IGenericMessage IntervalSettingNotPositive(ILogger logger, string settingName, int seconds);

    /// <summary>Logged when the poll interval a host declares at registration is not positive.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="interval">The declared interval.</param>
    /// <returns>The message.</returns>
    [MessageLogging(EventId = 61101, Level = LogLevel.Error,
        Message = "ConfigurationGatewayTypes.CacheChangePollInterval is {interval}, which is not positive; the host cannot start")]
    public static partial IGenericMessage DeclaredIntervalInvalid(ILogger logger, TimeSpan interval);

    /// <summary>Logged when the host's configurationSchema.json does not declare the container the tokens live in.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="container">The container.</param>
    /// <param name="path">Its path.</param>
    /// <param name="dataStore">The data store.</param>
    /// <param name="reason">Why it does not resolve.</param>
    /// <returns>The message.</returns>
    [MessageLogging(EventId = 61102, Level = LogLevel.Error,
        Message = "The container '{path}.{container}' the configuration change tokens live in is not declared in data store '{dataStore}' of configurationSchema.json ({reason}); the host cannot start")]
    public static partial IGenericMessage TokenContainerNotDeclared(ILogger logger, string container, string path, string dataStore, string reason);

    /// <summary>
    /// Logged when the host's platform store is the server tier, which no other host's cache reads: configuration changes are
    /// neither published nor looked at.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="connection">The platform connection.</param>
    /// <returns>The message.</returns>
    [MessageLogging(EventId = 11314, Level = LogLevel.Information,
        Message = "This host's platform store '{connection}' is the server tier, which no other host's cache reads; configuration changes are neither published to other hosts nor looked at from them")]
    public static partial IGenericMessage PlatformIsServerTier(ILogger logger, string connection);

    /// <summary>Logged when the poller stops.</summary>
    /// <param name="logger">The logger.</param>
    /// <returns>The message.</returns>
    [MessageLogging(EventId = 11313, Level = LogLevel.Information,
        Message = "Stopped looking at the configuration changes other hosts publish")]
    public static partial IGenericMessage PollerStopped(ILogger logger);
}
