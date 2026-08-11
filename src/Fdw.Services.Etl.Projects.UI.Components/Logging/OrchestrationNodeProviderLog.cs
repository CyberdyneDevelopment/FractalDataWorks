using System;
using Microsoft.Extensions.Logging;
using Fdw.Messages;
using Fdw.MessageLogging;

namespace Fdw.Services.Etl.Projects.UI.Components.Logging;

/// <summary>
/// MessageLogging methods for OrchestrationNodeProvider operations.
/// EventId range: 8840-8849
/// </summary>
[MessageLoggingTypeCode("UICOMPONENTS2")]
public static partial class OrchestrationNodeProviderLog
{
    // ═══════════════════════════════════════════════════════════════════════════
    // Load Root Nodes (8840-8841)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs when loading the root nodes list fails.</summary>
    [MessageLogging(EventId = 71000, Level = LogLevel.Warning,
        Message = "OrchestrationNodeProvider: Failed to load root nodes")]
    public static partial IGenericMessage LoadNodesFailed(
        ILogger logger);

    /// <summary>Logs when loading the root nodes list fails with exception.</summary>
    [MessageLogging(EventId = 71001, Level = LogLevel.Warning,
        Message = "OrchestrationNodeProvider: Failed to load root nodes")]
    public static partial IGenericMessage LoadNodesException(
        ILogger logger,
        Exception exception);

    // ═══════════════════════════════════════════════════════════════════════════
    // Get Node (8842-8843)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs when loading a node detail fails.</summary>
    [MessageLogging(EventId = 71002, Level = LogLevel.Warning,
        Message = "OrchestrationNodeProvider: Failed to load node '{nodeId}'")]
    public static partial IGenericMessage NodeDetailLoadFailed(
        ILogger logger,
        string nodeId);

    /// <summary>Logs when loading a node detail fails with exception.</summary>
    [MessageLogging(EventId = 71003, Level = LogLevel.Warning,
        Message = "OrchestrationNodeProvider: Failed to load node detail")]
    public static partial IGenericMessage NodeDetailLoadException(
        ILogger logger,
        Exception exception);

    // ═══════════════════════════════════════════════════════════════════════════
    // Create Node (8844-8845)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs when creating a node fails.</summary>
    [MessageLogging(EventId = 71004, Level = LogLevel.Warning,
        Message = "OrchestrationNodeProvider: Failed to create node")]
    public static partial IGenericMessage NodeCreateFailed(
        ILogger logger);

    /// <summary>Logs when creating a node fails with exception.</summary>
    [MessageLogging(EventId = 71005, Level = LogLevel.Warning,
        Message = "OrchestrationNodeProvider: Failed to create node")]
    public static partial IGenericMessage NodeCreateException(
        ILogger logger,
        Exception exception);

    // ═══════════════════════════════════════════════════════════════════════════
    // Update Node (8846-8847)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs when updating a node fails.</summary>
    [MessageLogging(EventId = 71006, Level = LogLevel.Warning,
        Message = "OrchestrationNodeProvider: Failed to update node '{nodeId}'")]
    public static partial IGenericMessage NodeUpdateFailed(
        ILogger logger,
        string nodeId);

    /// <summary>Logs when updating a node fails with exception.</summary>
    [MessageLogging(EventId = 71007, Level = LogLevel.Warning,
        Message = "OrchestrationNodeProvider: Failed to update node")]
    public static partial IGenericMessage NodeUpdateException(
        ILogger logger,
        Exception exception);

    // ═══════════════════════════════════════════════════════════════════════════
    // Delete Node (8848-8849)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs when deleting a node fails.</summary>
    [MessageLogging(EventId = 71008, Level = LogLevel.Warning,
        Message = "OrchestrationNodeProvider: Failed to delete node '{nodeId}'")]
    public static partial IGenericMessage NodeDeleteFailed(
        ILogger logger,
        string nodeId);

    /// <summary>Logs when deleting a node fails with exception.</summary>
    [MessageLogging(EventId = 71009, Level = LogLevel.Warning,
        Message = "OrchestrationNodeProvider: Failed to delete node")]
    public static partial IGenericMessage NodeDeleteException(
        ILogger logger,
        Exception exception);
}
