using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Tds.Endpoints.Logging;

/// <summary>
/// MessageLogging for the SQL Server endpoint: the listener, each connection, its login and the
/// statements it runs.
/// </summary>
/// <remarks>
/// A password, a session id and a row value never appear here at any level. A statement is logged by
/// its kind, not its text: the text can carry literals the caller filtered on.
/// </remarks>
[MessageLoggingTypeCode("SQLEP")]
public static partial class TdsEndpointLog
{
    /// <summary>Logs that the listener started.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="endpoint">The address and port it listens on.</param>
    /// <param name="serverName">The server name it presents.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11000, Level = LogLevel.Information,
        Message = "SQL Server endpoint '{serverName}' listening on {endpoint}")]
    public static partial IGenericMessage ListenerStarted(ILogger logger, string endpoint, string serverName);

    /// <summary>Logs that the listener stopped.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="endpoint">The address and port it listened on.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11001, Level = LogLevel.Information,
        Message = "SQL Server endpoint on {endpoint} stopped")]
    public static partial IGenericMessage ListenerStopped(ILogger logger, string endpoint);

    /// <summary>Logs that a client connected.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="remote">The client's address.</param>
    /// <param name="sessionId">The session id this server gave the connection.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11002, Level = LogLevel.Debug,
        Message = "Client {remote} connected as SPID {sessionId}")]
    public static partial IGenericMessage ConnectionAccepted(ILogger logger, string remote, int sessionId);

    /// <summary>Logs that a connection ended.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="sessionId">The connection's session id.</param>
    /// <param name="reason">Why it ended.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11003, Level = LogLevel.Debug,
        Message = "SPID {sessionId} closed: {reason}")]
    public static partial IGenericMessage ConnectionClosed(ILogger logger, int sessionId, string reason);

    /// <summary>Logs that a client signed in.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="sessionId">The connection's session id.</param>
    /// <param name="userName">The login name.</param>
    /// <param name="applicationName">The client application.</param>
    /// <param name="clientInterface">The client library.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11004, Level = LogLevel.Information,
        Message = "SPID {sessionId} signed in as '{userName}' from '{applicationName}' ({clientInterface})")]
    public static partial IGenericMessage LoginSucceeded(ILogger logger, int sessionId, string userName, string applicationName, string clientInterface);

    /// <summary>Logs that a request arrived.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="sessionId">The connection's session id.</param>
    /// <param name="kind">The request's kind: a batch, or the procedure an RPC names.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11005, Level = LogLevel.Trace,
        Message = "SPID {sessionId} request: {kind}")]
    public static partial IGenericMessage RequestReceived(ILogger logger, int sessionId, string kind);

    /// <summary>Logs that a statement ran.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="sessionId">The connection's session id.</param>
    /// <param name="statement">The statement's kind.</param>
    /// <param name="rows">The rows it returned.</param>
    /// <param name="elapsedMs">How long it took.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11006, Level = LogLevel.Debug,
        Message = "SPID {sessionId} ran {statement}: {rows} row(s) in {elapsedMs:F1} ms")]
    public static partial IGenericMessage StatementRan(ILogger logger, int sessionId, string statement, long rows, double elapsedMs);

    /// <summary>Logs that a query was sent to a table's store.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="schema">The table's schema.</param>
    /// <param name="table">The table.</param>
    /// <param name="filtered">Whether a filter was pushed down.</param>
    /// <param name="paged">Whether paging was pushed down.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11007, Level = LogLevel.Trace,
        Message = "Querying [{schema}].[{table}]: filter pushed down = {filtered}, paging pushed down = {paged}")]
    public static partial IGenericMessage QueryingTable(ILogger logger, string schema, string table, bool filtered, bool paged);

    /// <summary>Logs that the client cancelled the request in progress.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="sessionId">The connection's session id.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11008, Level = LogLevel.Debug,
        Message = "SPID {sessionId} attention: the request in progress is cancelled")]
    public static partial IGenericMessage AttentionReceived(ILogger logger, int sessionId);

    /// <summary>Logs that a statement was refused with a SQL Server error.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="sessionId">The connection's session id.</param>
    /// <param name="number">The SQL Server error number.</param>
    /// <param name="text">The error text the client receives.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 20001, Level = LogLevel.Debug,
        Message = "SPID {sessionId} refused a statement with error {number}: {text}")]
    public static partial IGenericMessage StatementRefused(ILogger logger, int sessionId, int number, string text);

    /// <summary>Logs that a connection's platform session was closed with it.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="sessionId">The connection's session id.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11009, Level = LogLevel.Debug,
        Message = "SPID {sessionId}: the platform session was closed with the connection")]
    public static partial IGenericMessage SessionClosed(ILogger logger, int sessionId);

    /// <summary>Logs that a connection's platform session could not be closed with it.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="sessionId">The connection's session id.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 50003, Level = LogLevel.Debug,
        Message = "SPID {sessionId}: the platform session was not closed with the connection (already ended, or the store did not answer); it ends at its own expiry")]
    public static partial IGenericMessage SessionNotClosed(ILogger logger, int sessionId);

    /// <summary>Logs that arithmetic overflowed its type, which the client receives as error 8115.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="ex">The overflow.</param>
    /// <param name="line">The line of the expression.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 20002, Level = LogLevel.Debug,
        Message = "Arithmetic on line {line} overflowed its type")]
    public static partial IGenericMessage ArithmeticOverflowed(ILogger logger, Exception ex, int line);

    /// <summary>Logs that a login was refused.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="userName">The login name presented.</param>
    /// <param name="reason">Why it was refused; never which of user or password was wrong.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 50000, Level = LogLevel.Warning,
        Message = "Login refused for '{userName}': {reason}")]
    public static partial IGenericMessage LoginRefused(ILogger logger, string userName, string reason);

    /// <summary>Logs that a batch's session could not be authenticated again.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="sessionId">The connection's session id.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 50002, Level = LogLevel.Warning,
        Message = "SPID {sessionId}: the session behind the connection is no longer valid; the connection is closed")]
    public static partial IGenericMessage SessionEnded(ILogger logger, int sessionId);

    /// <summary>Logs that the endpoint's configuration cannot be used.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="setting">The setting at fault.</param>
    /// <param name="reason">What is wrong with it.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 60004, Level = LogLevel.Critical,
        Message = "The SQL Server endpoint's {setting} is unusable: {reason}")]
    public static partial IGenericMessage ConfigurationInvalid(ILogger logger, string setting, string reason);

    /// <summary>Logs that the login flow does not deliver a session.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="flowName">The flow the endpoint is configured to run.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 60001, Level = LogLevel.Error,
        Message = "Login flow '{flowName}' delivers no session; a SQL Server connection needs a session-delivered flow")]
    public static partial IGenericMessage FlowDeliversNoSession(ILogger logger, string flowName);

    /// <summary>Logs that a connection ended on an unexpected exception.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="ex">The exception.</param>
    /// <param name="sessionId">The connection's session id.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 90000, Level = LogLevel.Error,
        Message = "SPID {sessionId} ended on an unexpected exception")]
    public static partial IGenericMessage ConnectionFaulted(ILogger logger, Exception ex, int sessionId);

    /// <summary>Logs that a statement asked for something this endpoint does not do.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="what">What was asked for.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 90004, Level = LogLevel.Debug,
        Message = "Not supported by the SQL Server endpoint: {what}")]
    public static partial IGenericMessage NotSupported(ILogger logger, string what);
}
