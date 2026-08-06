using System;
using Microsoft.Extensions.Logging;
using Fdw.Messages;
using Fdw.MessageLogging;

namespace Fdw.UI.DrillDown;

/// <summary>
/// MessageLogging methods for <see cref="ConfigurationDrillDownProvider"/> operations.
/// EventId range: 4650-4670
/// </summary>
[MessageLoggingTypeCode("DRILLDOWN")]
public static partial class ConfigurationDrillDownProviderLog
{
    // ═══════════════════════════════════════════════════════════════════════════
    // Trace (4650-4655)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs when metadata loading begins for a service category.</summary>
    [MessageLogging(EventId = 11000, Level = LogLevel.Trace,
        Message = "ConfigurationDrillDownProvider: Loading metadata for category '{serviceCategory}'")]
    public static partial IGenericMessage MetadataLoading(
        ILogger logger,
        string serviceCategory);

    /// <summary>Logs when the tree building process begins.</summary>
    [MessageLogging(EventId = 11001, Level = LogLevel.Trace,
        Message = "ConfigurationDrillDownProvider: Building tree from root data for '{instanceName}'")]
    public static partial IGenericMessage TreeBuilding(
        ILogger logger,
        string instanceName);

    /// <summary>Logs when a node is selected in the configuration tree.</summary>
    [MessageLogging(EventId = 11002, Level = LogLevel.Trace,
        Message = "ConfigurationDrillDownProvider: Node selected — '{nodeLabel}' (type: {nodeType})")]
    public static partial IGenericMessage NodeSelected(
        ILogger logger,
        string nodeLabel,
        string nodeType);

    /// <summary>Logs when a node is expanded in the configuration tree.</summary>
    [MessageLogging(EventId = 11003, Level = LogLevel.Trace,
        Message = "ConfigurationDrillDownProvider: Node expanded — '{nodeLabel}'")]
    public static partial IGenericMessage NodeExpanded(
        ILogger logger,
        string nodeLabel);

    /// <summary>Logs when a node is collapsed in the configuration tree.</summary>
    [MessageLogging(EventId = 11004, Level = LogLevel.Trace,
        Message = "ConfigurationDrillDownProvider: Node collapsed — '{nodeLabel}'")]
    public static partial IGenericMessage NodeCollapsed(
        ILogger logger,
        string nodeLabel);

    /// <summary>Logs when the context is rebuilt and passed to the consumer.</summary>
    [MessageLogging(EventId = 11005, Level = LogLevel.Trace,
        Message = "ConfigurationDrillDownProvider: Context rebuilt")]
    public static partial IGenericMessage ContextRebuilt(
        ILogger logger);

    // ═══════════════════════════════════════════════════════════════════════════
    // Debug (4656-4658)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs when metadata has been loaded with a type count.</summary>
    [MessageLogging(EventId = 11006, Level = LogLevel.Debug,
        Message = "ConfigurationDrillDownProvider: Metadata loaded — {typeCount} child types discovered")]
    public static partial IGenericMessage MetadataLoaded(
        ILogger logger,
        int typeCount);

    /// <summary>Logs when the tree has been built with a node count.</summary>
    [MessageLogging(EventId = 11007, Level = LogLevel.Debug,
        Message = "ConfigurationDrillDownProvider: Tree built with {nodeCount} top-level nodes")]
    public static partial IGenericMessage TreeBuilt(
        ILogger logger,
        int nodeCount);

    /// <summary>Logs when dropdown values have been loaded for a collection.</summary>
    [MessageLogging(EventId = 11008, Level = LogLevel.Debug,
        Message = "ConfigurationDrillDownProvider: Dropdown values loaded — {valueCount} values for '{collectionName}'")]
    public static partial IGenericMessage DropdownValuesLoaded(
        ILogger logger,
        int valueCount,
        string collectionName);

    // ═══════════════════════════════════════════════════════════════════════════
    // Info (4659-4660)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs when the configuration instance has been loaded and tree initialized.</summary>
    [MessageLogging(EventId = 11009, Level = LogLevel.Information,
        Message = "ConfigurationDrillDownProvider: Instance '{instanceName}' loaded, tree initialized")]
    public static partial IGenericMessage InstanceLoaded(
        ILogger logger,
        string instanceName);

    /// <summary>Logs when a refresh operation completes successfully.</summary>
    [MessageLogging(EventId = 11010, Level = LogLevel.Information,
        Message = "ConfigurationDrillDownProvider: Refresh completed for '{instanceName}'")]
    public static partial IGenericMessage RefreshCompleted(
        ILogger logger,
        string instanceName);

    // ═══════════════════════════════════════════════════════════════════════════
    // Warn (4661-4663)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs when a parent collection property contains no children.</summary>
    // Why Debug, not Warning (FDW-583): an empty child collection is normal leaf/empty UI state, not
    // an abnormal or actionable condition.
    [MessageLogging(EventId = 31000, Level = LogLevel.Debug,
        Message = "ConfigurationDrillDownProvider: Empty children for property '{propertyName}' on '{parentLabel}'")]
    public static partial IGenericMessage EmptyChildren(
        ILogger logger,
        string propertyName,
        string parentLabel);

    /// <summary>Logs when no type metadata was found for a table name.</summary>
    [MessageLogging(EventId = 31001, Level = LogLevel.Warning,
        Message = "ConfigurationDrillDownProvider: No metadata found for type '{tableName}'")]
    public static partial IGenericMessage NoMetadataForType(
        ILogger logger,
        string tableName);

    /// <summary>Logs when a property referenced by metadata was not found on the data object.</summary>
    [MessageLogging(EventId = 31002, Level = LogLevel.Warning,
        Message = "ConfigurationDrillDownProvider: Property '{propertyName}' not found on type '{typeName}'")]
    public static partial IGenericMessage PropertyNotFound(
        ILogger logger,
        string propertyName,
        string typeName);

    // ═══════════════════════════════════════════════════════════════════════════
    // Error (4664-4666)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs when metadata loading fails.</summary>
    [MessageLogging(EventId = 91000, Level = LogLevel.Error,
        Message = "ConfigurationDrillDownProvider: Failed to load metadata for category '{serviceCategory}'")]
    public static partial IGenericMessage MetadataLoadFailed(
        ILogger logger,
        Exception exception,
        string serviceCategory);

    /// <summary>Logs when tree building fails.</summary>
    [MessageLogging(EventId = 91001, Level = LogLevel.Error,
        Message = "ConfigurationDrillDownProvider: Failed to build tree for '{instanceName}'")]
    public static partial IGenericMessage TreeBuildFailed(
        ILogger logger,
        Exception exception,
        string instanceName);

    /// <summary>Logs when dropdown value loading fails.</summary>
    [MessageLogging(EventId = 91002, Level = LogLevel.Error,
        Message = "ConfigurationDrillDownProvider: Failed to load dropdown values for '{collectionName}'")]
    public static partial IGenericMessage DropdownLoadFailed(
        ILogger logger,
        Exception exception,
        string collectionName);
}
