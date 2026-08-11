using System;
using Microsoft.Extensions.Logging;
using Fdw.Messages;
using Fdw.MessageLogging;

namespace Fdw.Configuration.Components.Logging;

/// <summary>
/// MessageLogging methods for ConfigurationProvider operations.
/// Provider-specific messages with baked-in provider name and entity type.
/// EventId range: 8960-8979
/// </summary>
[MessageLoggingTypeCode("COMPONENTS3")]
public static partial class ConfigurationProviderLog
{
    // ═══════════════════════════════════════════════════════════════════════════
    // Load Instances (8960-8961)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs when loading configuration instances fails.</summary>
    [MessageLogging(EventId = 71000, Level = LogLevel.Error,
        Message = "ConfigurationProvider: Failed to load instances list")]
    public static partial IGenericMessage LoadInstancesFailed(
        ILogger logger);

    /// <summary>Logs when loading configuration instances fails with exception.</summary>
    [MessageLogging(EventId = 71001, Level = LogLevel.Error,
        Message = "ConfigurationProvider: Failed to load instances list")]
    public static partial IGenericMessage LoadInstancesException(
        ILogger logger,
        Exception exception);

    // ═══════════════════════════════════════════════════════════════════════════
    // Load Types (8962-8963)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs when loading configuration types fails.</summary>
    [MessageLogging(EventId = 71002, Level = LogLevel.Error,
        Message = "ConfigurationProvider: Failed to load types list")]
    public static partial IGenericMessage LoadTypesFailed(
        ILogger logger);

    /// <summary>Logs when loading configuration types fails with exception.</summary>
    [MessageLogging(EventId = 71003, Level = LogLevel.Error,
        Message = "ConfigurationProvider: Failed to load types list")]
    public static partial IGenericMessage LoadTypesException(
        ILogger logger,
        Exception exception);

    // ═══════════════════════════════════════════════════════════════════════════
    // Instance Detail (8964-8965)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs when loading instance detail fails.</summary>
    [MessageLogging(EventId = 71004, Level = LogLevel.Error,
        Message = "ConfigurationProvider: Failed to load instance detail for '{instanceName}'")]
    public static partial IGenericMessage InstanceDetailFailed(
        ILogger logger,
        string instanceName);

    /// <summary>Logs when loading instance detail fails with exception.</summary>
    [MessageLogging(EventId = 71005, Level = LogLevel.Error,
        Message = "ConfigurationProvider: Failed to load instance detail")]
    public static partial IGenericMessage InstanceDetailException(
        ILogger logger,
        Exception exception);

    // ═══════════════════════════════════════════════════════════════════════════
    // Create Instance (8966-8967)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs when creating a configuration instance fails.</summary>
    [MessageLogging(EventId = 71006, Level = LogLevel.Error,
        Message = "ConfigurationProvider: Failed to create instance")]
    public static partial IGenericMessage CreateInstanceFailed(
        ILogger logger);

    /// <summary>Logs when creating a configuration instance fails with exception.</summary>
    [MessageLogging(EventId = 71007, Level = LogLevel.Error,
        Message = "ConfigurationProvider: Failed to create instance")]
    public static partial IGenericMessage CreateInstanceException(
        ILogger logger,
        Exception exception);

    // ═══════════════════════════════════════════════════════════════════════════
    // Update Instance (8968-8969)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs when updating a configuration instance fails.</summary>
    [MessageLogging(EventId = 71008, Level = LogLevel.Error,
        Message = "ConfigurationProvider: Failed to update instance '{instanceName}'")]
    public static partial IGenericMessage UpdateInstanceFailed(
        ILogger logger,
        string instanceName);

    /// <summary>Logs when updating a configuration instance fails with exception.</summary>
    [MessageLogging(EventId = 71009, Level = LogLevel.Error,
        Message = "ConfigurationProvider: Failed to update instance")]
    public static partial IGenericMessage UpdateInstanceException(
        ILogger logger,
        Exception exception);

    // ═══════════════════════════════════════════════════════════════════════════
    // Delete Instance (8970-8971)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs when deleting a configuration instance fails.</summary>
    [MessageLogging(EventId = 71010, Level = LogLevel.Error,
        Message = "ConfigurationProvider: Failed to delete instance '{instanceName}'")]
    public static partial IGenericMessage DeleteInstanceFailed(
        ILogger logger,
        string instanceName);

    /// <summary>Logs when deleting a configuration instance fails with exception.</summary>
    [MessageLogging(EventId = 71011, Level = LogLevel.Error,
        Message = "ConfigurationProvider: Failed to delete instance")]
    public static partial IGenericMessage DeleteInstanceException(
        ILogger logger,
        Exception exception);

    // ═══════════════════════════════════════════════════════════════════════════
    // Root Types (8972-8973)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs when loading root configuration types fails.</summary>
    [MessageLogging(EventId = 71012, Level = LogLevel.Error,
        Message = "ConfigurationProvider: Failed to load root types")]
    public static partial IGenericMessage LoadRootTypesFailed(
        ILogger logger);

    /// <summary>Logs when loading root configuration types fails with exception.</summary>
    [MessageLogging(EventId = 71013, Level = LogLevel.Error,
        Message = "ConfigurationProvider: Failed to load root types")]
    public static partial IGenericMessage LoadRootTypesException(
        ILogger logger,
        Exception exception);
}
