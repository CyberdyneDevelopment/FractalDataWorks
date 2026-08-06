using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Versions.Logging;

/// <summary>
/// MessageLogging for Version Registry operations.
/// EventId range: 7020-7039
/// </summary>
[MessageLoggingTypeCode("VERSIONS")]
public static partial class VersionLog
{
    /// <summary> Logs that version discovery has started. </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="pattern">The assembly name pattern.</param>
    [MessageLogging(
        EventId = 11000,
        Level = LogLevel.Information,
        Message = "Version discovery started for assemblies matching '{pattern}'")]
    public static partial IGenericMessage DiscoveryStarted(
        ILogger logger,
        string pattern);

    /// <summary> Logs that version discovery has completed. </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="count">Number of assemblies found.</param>
    /// <param name="groupCount">Number of groups created.</param>
    [MessageLogging(
        EventId = 11001,
        Level = LogLevel.Information,
        Message = "Version discovery completed. Found {count} assemblies and {groupCount} groups.")]
    public static partial IGenericMessage DiscoveryCompleted(
        ILogger logger,
        int count,
        int groupCount);

    /// <summary> Logs that version discovery failed. </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="ex">The exception.</param>
    /// <param name="errorMessage">The error message.</param>
    [MessageLogging(
        EventId = 71000,
        Level = LogLevel.Error,
        Message = "Version discovery failed: {errorMessage}")]
    public static partial IGenericMessage DiscoveryFailed(
        ILogger logger,
        Exception ex,
        string errorMessage);

    /// <summary> Logs that a dominant version group was detected. </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="groupName">The name of the group.</param>
    /// <param name="version">The version string.</param>
    /// <param name="count">Number of assemblies in the group.</param>
    [MessageLogging(
        EventId = 11002,
        Level = LogLevel.Debug,
        Message = "Major group detected: '{groupName}' with version '{version}' ({count} assemblies)")]
    public static partial IGenericMessage GroupDetected(
        ILogger logger,
        string groupName,
        string version,
        int count);
}
