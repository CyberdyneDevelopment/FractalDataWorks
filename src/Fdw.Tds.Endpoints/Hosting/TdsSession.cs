using System;
using System.Collections.Generic;

namespace Fdw.Tds.Endpoints.Hosting;

/// <summary>
/// One connection's session: who signed in, the session they hold, and the state SQL Server keeps per
/// session — SET options, prepared statements, the transaction count.
/// </summary>
/// <remarks>
/// The SET options start at the values SQL Server gives every new session, and a client's
/// reset-connection request puts them back there. A value here is SQL Server's own session default, not a
/// value this server chose.
/// </remarks>
public sealed class TdsSession
{
    private readonly Dictionary<int, PreparedStatement> _prepared = [];
    private int _nextHandle = 1;

    /// <summary>Initializes a new instance of the <see cref="TdsSession"/> class.</summary>
    /// <param name="sessionId">The SPID this server gave the connection.</param>
    /// <param name="userName">The login name.</param>
    /// <param name="applicationName">The client application.</param>
    /// <param name="hostName">The client machine.</param>
    /// <param name="clientInterface">The client library.</param>
    /// <param name="sessionToken">The session id the login flow delivered; held only by this connection.</param>
    /// <param name="sessionServiceName">The session service the session belongs to.</param>
    /// <param name="database">The database the login opened.</param>
    public TdsSession(
        int sessionId,
        string userName,
        string applicationName,
        string hostName,
        string clientInterface,
        string sessionToken,
        string sessionServiceName,
        string database)
    {
        SessionId = sessionId;
        UserName = userName ?? throw new ArgumentNullException(nameof(userName));
        ApplicationName = applicationName ?? throw new ArgumentNullException(nameof(applicationName));
        HostName = hostName ?? throw new ArgumentNullException(nameof(hostName));
        ClientInterface = clientInterface ?? throw new ArgumentNullException(nameof(clientInterface));
        SessionToken = sessionToken ?? throw new ArgumentNullException(nameof(sessionToken));
        SessionServiceName = sessionServiceName ?? throw new ArgumentNullException(nameof(sessionServiceName));
        LoginDatabase = database ?? throw new ArgumentNullException(nameof(database));
        Reset();
    }

    /// <summary>Gets the SPID.</summary>
    public int SessionId { get; }

    /// <summary>Gets the login name.</summary>
    public string UserName { get; }

    /// <summary>Gets the client application.</summary>
    public string ApplicationName { get; }

    /// <summary>Gets the client machine.</summary>
    public string HostName { get; }

    /// <summary>Gets the client library.</summary>
    public string ClientInterface { get; }

    /// <summary>Gets the session id the login flow delivered.</summary>
    public string SessionToken { get; }

    /// <summary>Gets the session service the session belongs to.</summary>
    public string SessionServiceName { get; }

    /// <summary>Gets the database the login opened, which a reset of the session returns to.</summary>
    public string LoginDatabase { get; }

    /// <summary>Gets or sets the current database (USE).</summary>
    public string Database { get; set; } = string.Empty;

    /// <summary>Gets or sets the descriptor of the open transaction, which ENVCHANGE reports.</summary>
    public long TransactionDescriptor { get; set; }

    /// <summary>Gets or sets a value indicating whether row counts are left out of DONE tokens (SET NOCOUNT).</summary>
    public bool NoCount { get; set; }

    /// <summary>Gets or sets the most rows a SELECT returns, zero for no limit (SET ROWCOUNT).</summary>
    public long RowCountLimit { get; set; }

    /// <summary>Gets or sets a value indicating whether a SELECT answers its columns and no rows (SET FMTONLY).</summary>
    public bool FormatOnly { get; set; }

    /// <summary>Gets or sets a value indicating whether statements are parsed and not run (SET NOEXEC).</summary>
    public bool NoExec { get; set; }

    /// <summary>Gets or sets a value indicating whether double quotes delimit identifiers (SET QUOTED_IDENTIFIER).</summary>
    public bool QuotedIdentifier { get; set; }

    /// <summary>Gets or sets the most bytes of text a SELECT returns per value (SET TEXTSIZE).</summary>
    public int TextSize { get; set; }

    /// <summary>Gets or sets the first day of the week (SET DATEFIRST).</summary>
    public int DateFirst { get; set; }

    /// <summary>Gets or sets the lock timeout in milliseconds (SET LOCK_TIMEOUT).</summary>
    public int LockTimeout { get; set; }

    /// <summary>Gets or sets the transaction isolation level, as SQL Server names it.</summary>
    public string IsolationLevel { get; set; } = string.Empty;

    /// <summary>Gets or sets the open transaction count (<c>@@TRANCOUNT</c>).</summary>
    public int TransactionCount { get; set; }

    /// <summary>Gets or sets the rows the last statement returned (<c>@@ROWCOUNT</c>).</summary>
    public long LastRowCount { get; set; }

    /// <summary>Puts every SET option back to SQL Server's new-session value and drops prepared statements.</summary>
    public void Reset()
    {
        Database = LoginDatabase;
        NoCount = false;
        FormatOnly = false;
        NoExec = false;
        RowCountLimit = 0;
        QuotedIdentifier = true;
        TextSize = int.MaxValue;
        DateFirst = 7;
        LockTimeout = -1;
        IsolationLevel = "READ COMMITTED";
        TransactionCount = 0;
        LastRowCount = 0;
        _prepared.Clear();
    }

    /// <summary>Keeps a prepared statement and answers its handle.</summary>
    /// <param name="statement">The statement.</param>
    /// <returns>The handle sp_execute runs it by.</returns>
    public int Prepare(PreparedStatement statement)
    {
        var handle = _nextHandle++;
        _prepared[handle] = statement;
        return handle;
    }

    /// <summary>Finds a prepared statement by handle.</summary>
    /// <param name="handle">The handle.</param>
    /// <param name="statement">The statement, when the handle is live.</param>
    /// <returns>True when the handle is live.</returns>
    public bool TryGetPrepared(int handle, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out PreparedStatement? statement)
        => _prepared.TryGetValue(handle, out statement);

    /// <summary>Drops a prepared statement.</summary>
    /// <param name="handle">The handle.</param>
    /// <returns>True when the handle was live.</returns>
    public bool Unprepare(int handle) => _prepared.Remove(handle);
}
