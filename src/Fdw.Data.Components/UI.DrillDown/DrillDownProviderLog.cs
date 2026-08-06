using System;
using Microsoft.Extensions.Logging;
using Fdw.Messages;
using Fdw.MessageLogging;

namespace Fdw.UI.DrillDown;

/// <summary>
/// MessageLogging methods for <see cref="DrillDownProvider{T}"/> operations.
/// EventId range: 4630-4650
/// </summary>
[MessageLoggingTypeCode("DRILLDOWN")]
public static partial class DrillDownProviderLog
{
    // ═══════════════════════════════════════════════════════════════════════════
    // Trace (4630-4634)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs when a node is selected in the tree.</summary>
    [MessageLogging(EventId = 11011, Level = LogLevel.Trace,
        Message = "DrillDownProvider: Node selected — '{nodeLabel}' (type: {nodeType})")]
    public static partial IGenericMessage NodeSelected(
        ILogger logger,
        string nodeLabel,
        string nodeType);

    /// <summary>Logs when a node is expanded.</summary>
    [MessageLogging(EventId = 11012, Level = LogLevel.Trace,
        Message = "DrillDownProvider: Node expanded — '{nodeLabel}'")]
    public static partial IGenericMessage NodeExpanded(
        ILogger logger,
        string nodeLabel);

    /// <summary>Logs when a node is collapsed.</summary>
    [MessageLogging(EventId = 11013, Level = LogLevel.Trace,
        Message = "DrillDownProvider: Node collapsed — '{nodeLabel}'")]
    public static partial IGenericMessage NodeCollapsed(
        ILogger logger,
        string nodeLabel);

    /// <summary>Logs when the tree is rebuilt from the root object.</summary>
    [MessageLogging(EventId = 11014, Level = LogLevel.Trace,
        Message = "DrillDownProvider: Tree rebuilt from root")]
    public static partial IGenericMessage TreeRebuilt(
        ILogger logger);

    /// <summary>Logs when the context is rebuilt and passed to the consumer.</summary>
    [MessageLogging(EventId = 11015, Level = LogLevel.Trace,
        Message = "DrillDownProvider: Context rebuilt")]
    public static partial IGenericMessage ContextRebuilt(
        ILogger logger);

    // ═══════════════════════════════════════════════════════════════════════════
    // Debug (4635-4636)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs when the tree has been built with a node count.</summary>
    [MessageLogging(EventId = 11016, Level = LogLevel.Debug,
        Message = "DrillDownProvider: Tree built with {nodeCount} top-level nodes")]
    public static partial IGenericMessage TreeBuilt(
        ILogger logger,
        int nodeCount);

    /// <summary>Logs when the breadcrumb path has been computed.</summary>
    [MessageLogging(EventId = 11017, Level = LogLevel.Debug,
        Message = "DrillDownProvider: Breadcrumb computed with depth {depth}")]
    public static partial IGenericMessage BreadcrumbComputed(
        ILogger logger,
        int depth);

    // ═══════════════════════════════════════════════════════════════════════════
    // Info (4637-4638)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs when the root object has been loaded and tree initialized.</summary>
    [MessageLogging(EventId = 11018, Level = LogLevel.Information,
        Message = "DrillDownProvider: Root loaded, tree initialized")]
    public static partial IGenericMessage RootLoaded(
        ILogger logger);

    /// <summary>Logs when a refresh operation completes successfully.</summary>
    [MessageLogging(EventId = 11019, Level = LogLevel.Information,
        Message = "DrillDownProvider: Refresh completed")]
    public static partial IGenericMessage RefreshCompleted(
        ILogger logger);

    // ═══════════════════════════════════════════════════════════════════════════
    // Warn (4639-4640)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs when BuildTree produced an empty node list.</summary>
    // Why Debug, not Warning (FDW-583): an empty tree is normal UI state (e.g. a leaf object with no
    // drillable children) — not an abnormal or actionable condition.
    [MessageLogging(EventId = 31003, Level = LogLevel.Debug,
        Message = "DrillDownProvider: BuildTree returned an empty tree")]
    public static partial IGenericMessage EmptyTree(
        ILogger logger);

    /// <summary>Logs when BuildTree returned null instead of a node list.</summary>
    [MessageLogging(EventId = 91003, Level = LogLevel.Warning,
        Message = "DrillDownProvider: BuildTree delegate returned null")]
    public static partial IGenericMessage BuildTreeReturnedNull(
        ILogger logger);

    // ═══════════════════════════════════════════════════════════════════════════
    // Error (4641-4642)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs when the BuildTree delegate throws an exception.</summary>
    [MessageLogging(EventId = 91004, Level = LogLevel.Error,
        Message = "DrillDownProvider: BuildTree delegate failed")]
    public static partial IGenericMessage BuildTreeFailed(
        ILogger logger,
        Exception exception);

    /// <summary>Logs when a refresh operation fails.</summary>
    [MessageLogging(EventId = 91005, Level = LogLevel.Error,
        Message = "DrillDownProvider: Refresh failed")]
    public static partial IGenericMessage RefreshFailed(
        ILogger logger,
        Exception exception);
}
