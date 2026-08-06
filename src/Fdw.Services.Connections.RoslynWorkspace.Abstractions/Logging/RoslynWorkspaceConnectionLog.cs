using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Connections.RoslynWorkspace.Abstractions.Logging;

/// <summary>
/// MessageLogging for the RoslynWorkspace connection.
/// EventId range: 9600-9628 (9605-9608 trace, 9613-9616 info, 9625-9628 error reserved for FDW-437 symbol primitives).
/// </summary>
[MessageLoggingTypeCode("RW")]
public static partial class RoslynWorkspaceConnectionLog
{
    // ═══════════════════════════════════════════════════════════════════════════
    // Trace (9600-9604)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Logs that the RoslynWorkspace connection is loading a solution from the given path.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="connectionName">The name of the RoslynWorkspace connection.</param>
    /// <param name="solutionPath">The path of the solution being loaded.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11000, Level = LogLevel.Trace, Message = "RoslynWorkspace connection '{connectionName}' loading solution from '{solutionPath}'")]
    public static partial IGenericMessage LoadingSolution(ILogger logger, string connectionName, string solutionPath);

    /// <summary>
    /// Logs that the RoslynWorkspace connection is getting the source for the given symbol.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="connectionName">The name of the RoslynWorkspace connection.</param>
    /// <param name="symbolId">The identifier of the symbol whose source is being retrieved.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11001, Level = LogLevel.Trace, Message = "RoslynWorkspace connection '{connectionName}' getting symbol source for '{symbolId}'")]
    public static partial IGenericMessage GettingSymbolSource(ILogger logger, string connectionName, string symbolId);

    /// <summary>
    /// Logs that the RoslynWorkspace connection is building the workspace graph.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="connectionName">The name of the RoslynWorkspace connection.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11002, Level = LogLevel.Trace, Message = "RoslynWorkspace connection '{connectionName}' building workspace graph")]
    public static partial IGenericMessage BuildingGraph(ILogger logger, string connectionName);

    /// <summary>
    /// Logs that the RoslynWorkspace connection's snapshot workspace loaded and a command is being run.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="connectionName">The name of the RoslynWorkspace connection.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11003, Level = LogLevel.Trace, Message = "RoslynWorkspace connection '{connectionName}' snapshot workspace loaded, running command")]
    public static partial IGenericMessage SnapshotWorkspaceLoaded(ILogger logger, string connectionName);

    /// <summary>
    /// Logs that the RoslynWorkspace connection's snapshot workspace was disposed after a command.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="connectionName">The name of the RoslynWorkspace connection.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11004, Level = LogLevel.Trace, Message = "RoslynWorkspace connection '{connectionName}' snapshot workspace disposed after command")]
    public static partial IGenericMessage SnapshotWorkspaceDisposed(ILogger logger, string connectionName);

    /// <summary>
    /// Logs that the RoslynWorkspace connection is resolving the given symbol name.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="connectionName">The name of the RoslynWorkspace connection.</param>
    /// <param name="name">The symbol name being resolved.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11005, Level = LogLevel.Trace, Message = "RoslynWorkspace connection '{connectionName}' resolving symbol name '{name}'")]
    public static partial IGenericMessage ResolvingSymbol(ILogger logger, string connectionName, string name);

    /// <summary>
    /// Logs that the RoslynWorkspace connection is finding callers of the given symbol.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="connectionName">The name of the RoslynWorkspace connection.</param>
    /// <param name="symbolId">The identifier of the symbol whose callers are being found.</param>
    /// <param name="max">The maximum number of callers to return.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11006, Level = LogLevel.Trace, Message = "RoslynWorkspace connection '{connectionName}' finding callers of '{symbolId}' (max {max})")]
    public static partial IGenericMessage FindingCallers(ILogger logger, string connectionName, string symbolId, int max);

    /// <summary>
    /// Logs that the RoslynWorkspace connection is finding callees from the given symbol.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="connectionName">The name of the RoslynWorkspace connection.</param>
    /// <param name="symbolId">The identifier of the symbol whose callees are being found.</param>
    /// <param name="max">The maximum number of callees to return.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11007, Level = LogLevel.Trace, Message = "RoslynWorkspace connection '{connectionName}' finding callees from '{symbolId}' (max {max})")]
    public static partial IGenericMessage FindingCallees(ILogger logger, string connectionName, string symbolId, int max);

    /// <summary>
    /// Logs that the RoslynWorkspace connection is finding implementations of the given symbol.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="connectionName">The name of the RoslynWorkspace connection.</param>
    /// <param name="symbolId">The identifier of the symbol whose implementations are being found.</param>
    /// <param name="max">The maximum number of implementations to return.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11008, Level = LogLevel.Trace, Message = "RoslynWorkspace connection '{connectionName}' finding implementations of '{symbolId}' (max {max})")]
    public static partial IGenericMessage FindingImplementations(ILogger logger, string connectionName, string symbolId, int max);

    // ═══════════════════════════════════════════════════════════════════════════
    // Information (9610-9614)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Logs that the RoslynWorkspace connection was created in the given mode for the given solution path.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="connectionName">The name of the RoslynWorkspace connection.</param>
    /// <param name="mode">The mode the connection was created in (for example Live or Snapshot).</param>
    /// <param name="solutionPath">The path of the solution the connection was created for.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11009, Level = LogLevel.Information, Message = "RoslynWorkspace connection '{connectionName}' created in '{mode}' mode for '{solutionPath}'")]
    public static partial IGenericMessage Created(ILogger logger, string connectionName, string mode, string solutionPath);

    /// <summary>
    /// Logs that the RoslynWorkspace connection resolved the source for the given symbol.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="connectionName">The name of the RoslynWorkspace connection.</param>
    /// <param name="symbolId">The identifier of the symbol whose source was resolved.</param>
    /// <param name="charCount">The number of characters in the resolved source.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11010, Level = LogLevel.Information, Message = "RoslynWorkspace connection '{connectionName}' resolved symbol '{symbolId}' ({charCount} chars)")]
    public static partial IGenericMessage SymbolSourceResolved(ILogger logger, string connectionName, string symbolId, int charCount);

    /// <summary>
    /// Logs that the RoslynWorkspace connection finished building the workspace graph.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="connectionName">The name of the RoslynWorkspace connection.</param>
    /// <param name="nodeCount">The number of nodes in the built graph.</param>
    /// <param name="edgeCount">The number of edges in the built graph.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11011, Level = LogLevel.Information, Message = "RoslynWorkspace connection '{connectionName}' graph built: {nodeCount} nodes, {edgeCount} edges")]
    public static partial IGenericMessage GraphBuilt(ILogger logger, string connectionName, int nodeCount, int edgeCount);

    /// <summary>
    /// Logs that the RoslynWorkspace connection resolved a name to the given symbol.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="connectionName">The name of the RoslynWorkspace connection.</param>
    /// <param name="name">The name that was resolved.</param>
    /// <param name="symbolId">The identifier of the symbol the name resolved to.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11012, Level = LogLevel.Information, Message = "RoslynWorkspace connection '{connectionName}' resolved name '{name}' to symbol '{symbolId}'")]
    public static partial IGenericMessage SymbolResolved(ILogger logger, string connectionName, string name, string symbolId);

    /// <summary>
    /// Logs the number of callers found for the given symbol.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="connectionName">The name of the RoslynWorkspace connection.</param>
    /// <param name="symbolId">The identifier of the symbol whose callers were found.</param>
    /// <param name="count">The number of caller matches found.</param>
    /// <param name="max">The maximum number of callers requested.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11013, Level = LogLevel.Information, Message = "RoslynWorkspace connection '{connectionName}' callers of '{symbolId}': {count} match(es) (max {max})")]
    public static partial IGenericMessage CallersFound(ILogger logger, string connectionName, string symbolId, int count, int max);

    /// <summary>
    /// Logs the number of callees found for the given symbol.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="connectionName">The name of the RoslynWorkspace connection.</param>
    /// <param name="symbolId">The identifier of the symbol whose callees were found.</param>
    /// <param name="count">The number of callee matches found.</param>
    /// <param name="max">The maximum number of callees requested.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11014, Level = LogLevel.Information, Message = "RoslynWorkspace connection '{connectionName}' callees of '{symbolId}': {count} match(es) (max {max})")]
    public static partial IGenericMessage CalleesFound(ILogger logger, string connectionName, string symbolId, int count, int max);

    /// <summary>
    /// Logs the number of implementations found for the given symbol.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="connectionName">The name of the RoslynWorkspace connection.</param>
    /// <param name="symbolId">The identifier of the symbol whose implementations were found.</param>
    /// <param name="count">The number of implementation matches found.</param>
    /// <param name="max">The maximum number of implementations requested.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11015, Level = LogLevel.Information, Message = "RoslynWorkspace connection '{connectionName}' implementations of '{symbolId}': {count} match(es) (max {max})")]
    public static partial IGenericMessage ImplementationsFound(ILogger logger, string connectionName, string symbolId, int count, int max);

    // ═══════════════════════════════════════════════════════════════════════════
    // Warning (9618-9619)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Logs that workspace diagnostics reported warnings during the load of the given solution.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="connectionName">The name of the RoslynWorkspace connection.</param>
    /// <param name="diagnosticCount">The number of diagnostic warnings reported.</param>
    /// <param name="solutionPath">The path of the solution that was being loaded.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 71000, Level = LogLevel.Warning, Message = "RoslynWorkspace connection '{connectionName}': workspace diagnostics reported {diagnosticCount} warnings during load of '{solutionPath}'")]
    public static partial IGenericMessage WorkspaceDiagnosticsReported(ILogger logger, string connectionName, int diagnosticCount, string solutionPath);

    // ═══════════════════════════════════════════════════════════════════════════
    // Error (9620-9624)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Logs that factory validation failed for the RoslynWorkspace connection.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="connectionName">The name of the RoslynWorkspace connection.</param>
    /// <param name="reason">The reason factory validation failed.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 21000, Level = LogLevel.Error, Message = "RoslynWorkspace connection '{connectionName}': factory validation failed — {reason}")]
    public static partial IGenericMessage FactoryValidationFailed(ILogger logger, string connectionName, string reason);

    /// <summary>
    /// Logs that the RoslynWorkspace connection failed to load the workspace for the given solution.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="exception">The exception that was raised while loading the workspace.</param>
    /// <param name="connectionName">The name of the RoslynWorkspace connection.</param>
    /// <param name="solutionPath">The path of the solution whose workspace failed to load.</param>
    /// <param name="message">The failure message describing the load error.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 70000, Level = LogLevel.Error, Message = "RoslynWorkspace connection '{connectionName}': workspace load failed for '{solutionPath}': {message}")]
    public static partial IGenericMessage WorkspaceLoadFailed(ILogger logger, Exception exception, string connectionName, string solutionPath, string message);

    /// <summary>
    /// Logs that the given symbol was not found in the workspace.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="connectionName">The name of the RoslynWorkspace connection.</param>
    /// <param name="symbolId">The identifier of the symbol that was not found.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 31000, Level = LogLevel.Error, Message = "RoslynWorkspace connection '{connectionName}': symbol '{symbolId}' not found in workspace")]
    public static partial IGenericMessage SymbolNotFound(ILogger logger, string connectionName, string symbolId);

    /// <summary>
    /// Logs that the given symbol id is invalid.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="connectionName">The name of the RoslynWorkspace connection.</param>
    /// <param name="symbolId">The invalid symbol identifier.</param>
    /// <param name="reason">The reason the symbol identifier is invalid.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 20001, Level = LogLevel.Error, Message = "RoslynWorkspace connection '{connectionName}': invalid symbol id '{symbolId}' — {reason}")]
    public static partial IGenericMessage InvalidSymbolId(ILogger logger, string connectionName, string symbolId, string reason);

    /// <summary>
    /// Logs that the requested operation requires Live mode but the connection is currently in Snapshot mode.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="connectionName">The name of the RoslynWorkspace connection.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 40000, Level = LogLevel.Error, Message = "RoslynWorkspace connection '{connectionName}': operation requires Live mode but current mode is Snapshot")]
    public static partial IGenericMessage ModeRequiresLive(ILogger logger, string connectionName);

    /// <summary>
    /// Logs that the given name did not match any symbol in the workspace.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="connectionName">The name of the RoslynWorkspace connection.</param>
    /// <param name="name">The name that did not resolve to a symbol.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 31001, Level = LogLevel.Error, Message = "RoslynWorkspace connection '{connectionName}': name '{name}' did not match any symbol in workspace")]
    public static partial IGenericMessage SymbolNameUnresolved(ILogger logger, string connectionName, string name);

    /// <summary>
    /// Logs that the find-callers operation failed for the given symbol.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="exception">The exception that was raised while finding callers.</param>
    /// <param name="connectionName">The name of the RoslynWorkspace connection.</param>
    /// <param name="symbolId">The identifier of the symbol whose callers were being found.</param>
    /// <param name="message">The failure message describing the error.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 91000, Level = LogLevel.Error, Message = "RoslynWorkspace connection '{connectionName}': FindCallers failed for '{symbolId}': {message}")]
    public static partial IGenericMessage FindCallersFailed(ILogger logger, Exception exception, string connectionName, string symbolId, string message);

    /// <summary>
    /// Logs that the find-callees operation failed for the given symbol.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="exception">The exception that was raised while finding callees.</param>
    /// <param name="connectionName">The name of the RoslynWorkspace connection.</param>
    /// <param name="symbolId">The identifier of the symbol whose callees were being found.</param>
    /// <param name="message">The failure message describing the error.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 91001, Level = LogLevel.Error, Message = "RoslynWorkspace connection '{connectionName}': FindCallees failed for '{symbolId}': {message}")]
    public static partial IGenericMessage FindCalleesFailed(ILogger logger, Exception exception, string connectionName, string symbolId, string message);

    /// <summary>
    /// Logs that the find-implementations operation failed for the given symbol.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="exception">The exception that was raised while finding implementations.</param>
    /// <param name="connectionName">The name of the RoslynWorkspace connection.</param>
    /// <param name="symbolId">The identifier of the symbol whose implementations were being found.</param>
    /// <param name="message">The failure message describing the error.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 91002, Level = LogLevel.Error, Message = "RoslynWorkspace connection '{connectionName}': FindImplementations failed for '{symbolId}': {message}")]
    public static partial IGenericMessage FindImplementationsFailed(ILogger logger, Exception exception, string connectionName, string symbolId, string message);
}
