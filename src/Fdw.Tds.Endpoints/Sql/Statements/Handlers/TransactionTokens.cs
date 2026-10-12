using System;
using System.Buffers.Binary;
using Fdw.Protocols.Tds;

namespace Fdw.Tds.Endpoints.Sql.Statements.Handlers;

/// <summary>
/// The ENVCHANGE tokens that tell a client a transaction began or ended.
/// </summary>
/// <remarks>
/// Nothing here writes, so a transaction holds no work; it is kept so a client that begins, commits and
/// checks @@TRANCOUNT sees exactly what SQL Server would show it.
/// </remarks>
internal static class TransactionTokens
{
    /// <summary>Opens a transaction level and, for the outermost, reports it.</summary>
    /// <param name="context">The request.</param>
    public static void Begin(SqlExecutionContext context)
    {
        var session = context.Session;
        session.TransactionCount++;
        if (session.TransactionCount != 1)
            return;

        session.TransactionDescriptor = ((long)session.SessionId << 32) | (uint)Environment.TickCount;
        TdsTokenWriter.EnvironmentChange(context.Response.Payload, TdsEnvironmentChangeTypes.BeginTransaction, Descriptor(session.TransactionDescriptor), []);
    }

    /// <summary>Closes every transaction level and reports the end.</summary>
    /// <param name="context">The request.</param>
    /// <param name="committed">Whether it was committed rather than rolled back.</param>
    public static void End(SqlExecutionContext context, bool committed)
    {
        var session = context.Session;
        var type = committed ? TdsEnvironmentChangeTypes.CommitTransaction : TdsEnvironmentChangeTypes.RollbackTransaction;
        TdsTokenWriter.EnvironmentChange(context.Response.Payload, type, [], Descriptor(session.TransactionDescriptor));
        session.TransactionCount = 0;
        session.TransactionDescriptor = 0;
    }

    private static byte[] Descriptor(long descriptor)
    {
        var bytes = new byte[8];
        BinaryPrimitives.WriteInt64LittleEndian(bytes, descriptor);
        return bytes;
    }
}
