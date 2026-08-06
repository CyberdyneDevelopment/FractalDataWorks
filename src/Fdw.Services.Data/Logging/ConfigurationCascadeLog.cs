using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Logging;

/// <summary>
/// MessageLogging for configuration cascade save operations in DataGatewayService.
/// EventId range: 5560-5566
/// </summary>
[MessageLoggingTypeCode("DATA")]
public static partial class ConfigurationCascadeLog
{
    /// <summary>
    /// Logs the start of a cascade save operation.
    /// </summary>
    [MessageLogging(
        EventId = 11006,
        Level = LogLevel.Information,
        Message = "Cascade save beginning for '{rootTypeName}' — chain depth {chainDepth}")]
    public static partial IGenericMessage CascadeBegin(
        ILogger logger,
        string rootTypeName,
        int chainDepth);

    /// <summary>
    /// Logs execution of a single cascade level.
    /// </summary>
    [MessageLogging(
        EventId = 11007,
        Level = LogLevel.Trace,
        Message = "Cascade save level {levelIndex} — schema='{schema}', table='{table}'")]
    public static partial IGenericMessage CascadeLevel(
        ILogger logger,
        int levelIndex,
        string schema,
        string table);

    /// <summary>
    /// Logs successful completion of the entire cascade.
    /// </summary>
    [MessageLogging(
        EventId = 11008,
        Level = LogLevel.Information,
        Message = "Cascade save succeeded for '{rootTypeName}' — chain depth {chainDepth}, elapsed {durationMs}ms")]
    public static partial IGenericMessage CascadeSucceeded(
        ILogger logger,
        string rootTypeName,
        int chainDepth,
        double durationMs);

    /// <summary>
    /// Logs a failure at a specific cascade level (triggers rollback).
    /// </summary>
    [MessageLogging(
        EventId = 71000,
        Level = LogLevel.Error,
        Message = "Cascade save failed at level {level} — transaction will roll back")]
    public static partial IGenericMessage CascadeLevelFailed(
        ILogger logger,
        Exception exception,
        int level);

    /// <summary>
    /// Logs when a type is missing the required ManagedConfiguration metadata in ConfigurationTypes.
    /// </summary>
    [MessageLogging(
        EventId = 61000,
        Level = LogLevel.Critical,
        Message = "Type '{typeName}' has no ConfigurationTypes entry — cannot execute cascade save. Ensure [ManagedConfiguration] is applied and the assembly is loaded.")]
    public static partial IGenericMessage ManagedConfigurationAttributeMissing(
        ILogger logger,
        string typeName);

    /// <summary>
    /// Logs when a cycle is detected in the ParentTableName chain.
    /// </summary>
    // Why Error, not Critical (FDW-583): one save is aborted; the host process is unaffected and
    // survives. NOTE: this method has zero call sites (dead) as of this audit — flagged, not deleted
    // (out of scope for a severity-only pass).
    [MessageLogging(
        EventId = 41000,
        Level = LogLevel.Error,
        Message = "Cycle detected in cascade chain for type '{typeName}' — aborting save to prevent infinite loop")]
    public static partial IGenericMessage CascadeChainCycleDetected(
        ILogger logger,
        string typeName);

    /// <summary>
    /// Logs when a parent container cannot be resolved for a cascade level.
    /// </summary>
    [MessageLogging(
        EventId = 31002,
        Level = LogLevel.Error,
        Message = "Cascade save cannot resolve container for level '{schema}.{table}' — transaction will roll back")]
    public static partial IGenericMessage CascadeContainerNotResolved(
        ILogger logger,
        string schema,
        string table);
}
