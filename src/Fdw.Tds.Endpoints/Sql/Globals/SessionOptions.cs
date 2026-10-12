using System;

namespace Fdw.Tds.Endpoints.Sql.Globals;

/// <summary>
/// The bit mask <c>@@OPTIONS</c> answers.
/// </summary>
/// <remarks>
/// SQL Server sets one bit per user option. The options a client switches on at connect and this
/// endpoint holds to — ANSI_WARNINGS, ANSI_PADDING, ANSI_NULLS, ARITHABORT, ANSI_NULL_DFLT_ON and
/// CONCAT_NULL_YIELDS_NULL — are always set, because they are how this endpoint evaluates every
/// statement; NOCOUNT and QUOTED_IDENTIFIER follow the session.
/// </remarks>
public static class SessionOptions
{
    private const int AnsiWarnings = 8;
    private const int AnsiPadding = 16;
    private const int AnsiNulls = 32;
    private const int ArithAbort = 64;
    private const int QuotedIdentifier = 256;
    private const int NoCount = 512;
    private const int AnsiNullDefaultOn = 1024;
    private const int ConcatNullYieldsNull = 4096;

    /// <summary>Gets the mask for a session.</summary>
    /// <param name="session">The session.</param>
    /// <returns>The mask.</returns>
    public static int Mask(Hosting.TdsSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        var mask = AnsiWarnings | AnsiPadding | AnsiNulls | ArithAbort | AnsiNullDefaultOn | ConcatNullYieldsNull;
        if (session.QuotedIdentifier)
            mask |= QuotedIdentifier;

        if (session.NoCount)
            mask |= NoCount;

        return mask;
    }
}
