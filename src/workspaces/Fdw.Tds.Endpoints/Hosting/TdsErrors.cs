using System;
using Fdw.Protocols.Tds;
using Fdw.Results;
using Fdw.Tds.Endpoints.Logging;
using Fdw.Tds.Endpoints.Sql;

namespace Fdw.Tds.Endpoints.Hosting;

/// <summary>
/// Writes the ERROR a failed request ends with: the SQL Server error a refusal carries, or error 50000 with
/// the platform's own code for any other failure.
/// </summary>
public static class TdsErrors
{
    /// <summary>Writes the error for a failure.</summary>
    /// <param name="context">The request.</param>
    /// <param name="failure">The failed result.</param>
    /// <param name="line">The line it refers to, when the failure carries none.</param>
    public static void Write(SqlExecutionContext context, IGenericResult failure, int line)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(failure);
        var message = SqlRefusal.TryGetMessage(failure, out var refusal) ? refusal : SqlServerErrors.PlatformFailure(failure, line);
        TdsEndpointLog.StatementRefused(context.Logger, context.Session.SessionId, message.Number, message.Text);
        TdsTokenWriter.Message(context.Response.Payload, message, context.Server.ServerName);
    }
}
