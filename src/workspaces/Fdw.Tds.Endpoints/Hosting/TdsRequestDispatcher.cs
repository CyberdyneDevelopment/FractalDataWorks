using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Protocols.Tds;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Authentication.Abstractions.Sessions;
using Fdw.Tds.Endpoints.Logging;
using Fdw.Tds.Endpoints.Sql;
using Fdw.Tds.Endpoints.Sql.Procedures;
using Fdw.Tds.Endpoints.Sql.Statements;
using Fdw.Tds.Endpoints.Sql.Statements.Handlers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Fdw.Tds.Endpoints.Hosting;

/// <summary>
/// Answers one request — a SQL batch, an RPC, a transaction manager request — as the session's caller.
/// </summary>
/// <remarks>
/// <para>
/// Each request runs in its own DI scope, as an HTTP request does. The session the connection holds is
/// checked again first — expiry, revocation, the user-wide revocation that ends every token — and its
/// principal is set on the ambient authentication context for the request's length, so row-level security
/// and every access check see this caller and no other. A session that no longer stands ends the connection.
/// </para>
/// <para>
/// A request the client cancels mid-way ends in a DONE that acknowledges the attention; one that fails ends in
/// the error and a DONE that says so.
/// </para>
/// </remarks>
public sealed class TdsRequestDispatcher
{
    private const int FlushThreshold = 64 * 1024;
    private const string AuthenticationType = "SqlServer";

    private static readonly Dictionary<byte, Func<TdsMessage, SqlExecutionContext, CancellationToken, Task>> Requests = new()
    {
        [TdsPacketTypes.SqlBatch] = static (m, c, t) => Batch(m, c, t),
        [TdsPacketTypes.Rpc] = static (m, c, t) => Rpc(m, c, t),
        [TdsPacketTypes.TransactionManager] = static (m, c, _) => TransactionManager(m, c),
    };

    private static readonly Dictionary<ushort, Func<SqlExecutionContext, IGenericResult>> TransactionRequests = new()
    {
        [TransactionManagerRequest.BeginTransaction] = static c => Transactions(c, begin: true, committed: false),
        [TransactionManagerRequest.CommitTransaction] = static c => Transactions(c, begin: false, committed: true),
        [TransactionManagerRequest.RollbackTransaction] = static c => Transactions(c, begin: false, committed: false),
    };

    private readonly TdsEndpointOptions _options;
    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="TdsRequestDispatcher"/> class.</summary>
    /// <param name="options">The endpoint's options.</param>
    /// <param name="scopes">Creates each request's DI scope.</param>
    /// <param name="logger">The logger.</param>
    public TdsRequestDispatcher(TdsEndpointOptions options, IServiceScopeFactory scopes, ILogger logger)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _scopes = scopes ?? throw new ArgumentNullException(nameof(scopes));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>Answers a request.</summary>
    /// <param name="message">The request message.</param>
    /// <param name="session">The connection's session.</param>
    /// <param name="writer">The connection's message writer.</param>
    /// <param name="request">The request's token, which the client's attention cancels.</param>
    /// <param name="connection">The connection's token, which writes honour.</param>
    /// <returns>Whether the response acknowledged an attention; a failure ends the connection.</returns>
    public async Task<IGenericResult<bool>> Answer(
        TdsMessage message, TdsSession session, TdsMessageWriter writer, CancellationToken request, CancellationToken connection)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(session);
        var response = new TdsResponse(writer, FlushThreshold, connection);
        if (message.ResetsConnection)
        {
            session.Reset();
            TdsTokenWriter.EnvironmentChange(response.Payload, TdsEnvironmentChangeTypes.ResetConnection, [], []);
        }

        await using var scope = _scopes.CreateAsyncScope();
        try
        {
            return await AnswerAs(message, session, scope.ServiceProvider, response, request).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (request.IsCancellationRequested && !connection.IsCancellationRequested)
        {
            TdsEndpointLog.AttentionReceived(_logger, session.SessionId);
            TdsTokenWriter.Done(response.Payload, TdsTokenTypes.Done, TdsDoneStatus.Attention, 0, 0);
            return Sent(await response.Complete().ConfigureAwait(false), acknowledged: true);
        }
    }

    private async Task<IGenericResult<bool>> AnswerAs(
        TdsMessage message, TdsSession session, IServiceProvider services, TdsResponse response, CancellationToken request)
    {
        var caller = await services.GetRequiredService<ISessionAuthenticator>()
            .Authenticate(session.SessionToken, session.SessionServiceName, AuthenticationType, request).ConfigureAwait(false);
        if (caller.IsFailure)
            return await EndSession(session, response).ConfigureAwait(false);

        var accessor = services.GetRequiredService<IAuthenticationContextAccessor>();
        accessor.Current = new ClaimsPrincipalAuthenticationContext(caller.Value!.Principal);
        try
        {
            var context = new SqlExecutionContext(session, _options.Identity, services.GetRequiredService<TdsEndpointBase>(), response, _logger);
            await Dispatch(message, context, request).ConfigureAwait(false);
            return Sent(await response.Complete().ConfigureAwait(false), acknowledged: false);
        }
        finally
        {
            accessor.Current = null;
        }
    }

    private static IGenericResult<bool> Sent(IGenericResult sent, bool acknowledged)
        => sent.IsFailure ? sent.ToNewResult<bool>() : GenericResult<bool>.Success(acknowledged);

    private static Task Dispatch(TdsMessage message, SqlExecutionContext context, CancellationToken cancellationToken)
    {
        if (Requests.TryGetValue(message.PacketType, out var answer))
            return answer(message, context, cancellationToken);

        Fail(context, SqlRefusal.Refuse(SqlServerErrors.NotSupported($"TDS packet type 0x{message.PacketType:X2}", 1), context.Logger), TdsTokenTypes.Done);
        return Task.CompletedTask;
    }

    private static async Task Batch(TdsMessage message, SqlExecutionContext context, CancellationToken cancellationToken)
    {
        var batch = SqlBatchRequest.Parse(message.Payload, context.Logger);
        if (batch.IsFailure)
        {
            Fail(context, batch, TdsTokenTypes.Done);
            return;
        }

        TdsEndpointLog.RequestReceived(context.Logger, context.Session.SessionId, "SQL batch");
        var ran = await SqlBatchRunner.Run(batch.Value!.Text, context, cancellationToken).ConfigureAwait(false);
        End(context, ran, TdsTokenTypes.Done);
    }

    private static async Task Rpc(TdsMessage message, SqlExecutionContext context, CancellationToken cancellationToken)
    {
        var rpc = RpcRequest.Parse(message.Payload, context.Logger);
        if (rpc.IsFailure)
        {
            Fail(context, rpc, TdsTokenTypes.DoneProcedure);
            return;
        }

        TdsEndpointLog.RequestReceived(context.Logger, context.Session.SessionId, rpc.Value!.ProcedureName);
        if (!SqlProcedures.TryFind(rpc.Value.ProcedureName, out var procedure))
        {
            Fail(context, SqlRefusal.Refuse(SqlServerErrors.ProcedureNotFound(rpc.Value.ProcedureName, 1), context.Logger), TdsTokenTypes.DoneProcedure);
            return;
        }

        var arguments = rpc.Value.Parameters.Select((p, i) => new SqlProcedureArgument(i, p.Name, p.Value, p.IsOutput)).ToList();
        context.InProcedure = true;
        var result = await procedure.Execute(new SqlProcedureCall(context, procedure.SqlName, arguments, 1), cancellationToken).ConfigureAwait(false);
        if (result.IsFailure)
        {
            End(context, result, TdsTokenTypes.DoneProcedure);
            return;
        }

        TdsTokenWriter.ReturnStatus(context.Response.Payload, 0);
        End(context, ReturnValues(context, result.Value!), TdsTokenTypes.DoneProcedure);
    }

    // OUTPUT arguments travel back as RETURNVALUE tokens, after the procedure's status and before its DONEPROC,
    // in the order SQL Server sends them.
    private static IGenericResult ReturnValues(SqlExecutionContext context, SqlProcedureResult result)
    {
        var payload = context.Response.Payload;
        foreach (var output in result.Outputs)
        {
            var mark = payload.Mark();
            if (!TdsTokenWriter.TryReturnValue(payload, (ushort)output.Ordinal, output.Name, output.Type, output.Value, context.Server.Collation))
            {
                payload.TruncateTo(mark);
                return SqlRefusal.Refuse(SqlServerErrors.ConversionFailed(output.Type.ToString(), 1), context.Logger);
            }
        }

        return GenericResult.Success();
    }

    private static Task TransactionManager(TdsMessage message, SqlExecutionContext context)
    {
        var request = TransactionManagerRequest.Parse(message.Payload, context.Logger);
        if (request.IsFailure)
        {
            Fail(context, request, TdsTokenTypes.Done);
            return Task.CompletedTask;
        }

        var handled = TransactionRequests.TryGetValue(request.Value!.RequestType, out var handle)
            ? handle(context)
            : SqlRefusal.Refuse(SqlServerErrors.NotSupported($"transaction manager request {request.Value.RequestType}", 1), context.Logger);
        End(context, handled, TdsTokenTypes.Done);
        return Task.CompletedTask;
    }

    private static IGenericResult Transactions(SqlExecutionContext context, bool begin, bool committed)
    {
        if (begin)
        {
            TransactionTokens.Begin(context);
            return GenericResult.Success();
        }

        if (context.Session.TransactionCount == 0)
        {
            var number = committed ? 3902 : 3903;
            var verb = committed ? "COMMIT" : "ROLLBACK";
            return SqlRefusal.Refuse(new SqlServerMessage(number, 1, 16, $"The {verb} TRANSACTION request has no corresponding BEGIN TRANSACTION.", 1), context.Logger);
        }

        TransactionTokens.End(context, committed);
        return GenericResult.Success();
    }

    private static void End(SqlExecutionContext context, IGenericResult outcome, byte doneToken)
    {
        if (outcome.IsFailure)
        {
            Fail(context, outcome, doneToken);
            return;
        }

        TdsTokenWriter.Done(context.Response.Payload, doneToken, TransactionBit(context), 0, 0);
    }

    private static void Fail(SqlExecutionContext context, IGenericResult failure, byte doneToken)
    {
        TdsErrors.Write(context, failure, 1);
        TdsTokenWriter.Done(context.Response.Payload, doneToken, (ushort)(TdsDoneStatus.Error | TransactionBit(context)), 0, 0);
    }

    private static ushort TransactionBit(SqlExecutionContext context)
        => context.Session.TransactionCount > 0 ? TdsDoneStatus.InTransaction : TdsDoneStatus.Final;

    // The session behind the connection expired or was revoked: the client is told, and the connection ends.
    private async Task<IGenericResult<bool>> EndSession(TdsSession session, TdsResponse response)
    {
        var ended = TdsEndpointLog.SessionEnded(_logger, session.SessionId);
        TdsTokenWriter.Message(response.Payload, new SqlServerMessage(18456, 2, 14,
            "The session behind this connection is no longer valid. Sign in again.", 1), _options.Identity.ServerName);
        TdsTokenWriter.Done(response.Payload, TdsTokenTypes.Done, TdsDoneStatus.Error, 0, 0);
        await response.Complete().ConfigureAwait(false);
        return GenericResult<bool>.Failure(ended);
    }
}
