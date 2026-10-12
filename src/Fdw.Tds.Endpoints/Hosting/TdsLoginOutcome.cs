using System;
using System.Collections.Generic;
using Fdw.Protocols.Tds;

namespace Fdw.Tds.Endpoints.Hosting;

/// <summary>
/// How a login ended: a session and the TDS version agreed, or the errors the client is refused with.
/// </summary>
/// <remarks>
/// A refused login is not a failure of this server — it is an answer the client receives — so it is an
/// outcome with the SQL Server errors to send, not a failed result.
/// </remarks>
public sealed class TdsLoginOutcome
{
    private TdsLoginOutcome(TdsSession? session, uint tdsVersion, IReadOnlyList<SqlServerMessage> errors)
    {
        Session = session;
        TdsVersion = tdsVersion;
        Errors = errors;
    }

    /// <summary>Gets the session, when the login was accepted.</summary>
    public TdsSession? Session { get; }

    /// <summary>Gets the TDS version agreed, when the login was accepted.</summary>
    public uint TdsVersion { get; }

    /// <summary>Gets the errors the client is refused with, when the login was refused.</summary>
    public IReadOnlyList<SqlServerMessage> Errors { get; }

    /// <summary>An accepted login.</summary>
    /// <param name="session">The session.</param>
    /// <param name="tdsVersion">The TDS version agreed.</param>
    /// <returns>The outcome.</returns>
    public static TdsLoginOutcome SignedIn(TdsSession session, uint tdsVersion)
        => new(session ?? throw new ArgumentNullException(nameof(session)), tdsVersion, []);

    /// <summary>A refused login.</summary>
    /// <param name="errors">The errors the client is refused with.</param>
    /// <returns>The outcome.</returns>
    public static TdsLoginOutcome Refused(IReadOnlyList<SqlServerMessage> errors) => new(null, 0, errors);
}
