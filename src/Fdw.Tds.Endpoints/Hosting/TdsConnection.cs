using System;
using System.IO;
using System.Net.Security;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Fdw.Protocols.Tds;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Sessions;
using Fdw.Tds.Endpoints.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Fdw.Tds.Endpoints.Hosting;

/// <summary>
/// One client connection, from its first byte to its close: the handshake, the login, then its requests in
/// order until the client leaves or the server stops.
/// </summary>
/// <remarks>
/// <para>
/// A reader runs beside the requests for the connection's whole life, because the client can send an
/// attention signal while a request is still being answered and expects it to stop that request. Every other
/// message waits its turn; requests are answered one at a time and in the order they arrived, as SQL Server
/// answers a connection without MARS.
/// </para>
/// <para>
/// The handshake and login must finish within <see cref="TdsEndpointOptions.LoginTimeout"/>. Anything the
/// connection cannot carry on from — a malformed message, a second login, a session that ended — closes it,
/// as SQL Server closes a connection it cannot continue.
/// </para>
/// </remarks>
public sealed class TdsConnection
{
    private readonly TdsEndpointOptions _options;
    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger _logger;
    private readonly TdsRequestDispatcher _dispatcher;

    /// <summary>Initializes a new instance of the <see cref="TdsConnection"/> class.</summary>
    /// <param name="options">The endpoint's options.</param>
    /// <param name="scopes">Creates the login's and each request's DI scope.</param>
    /// <param name="logger">The logger.</param>
    public TdsConnection(TdsEndpointOptions options, IServiceScopeFactory scopes, ILogger logger)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _scopes = scopes ?? throw new ArgumentNullException(nameof(scopes));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _dispatcher = new TdsRequestDispatcher(options, scopes, logger);
    }

    /// <summary>Serves a connection until it closes.</summary>
    /// <param name="network">The connection's stream; the caller keeps ownership of it.</param>
    /// <param name="sessionId">The SPID the connection is given.</param>
    /// <param name="stopping">The server's stopping token.</param>
    /// <returns>Why the connection ended.</returns>
    public async Task<string> Serve(Stream network, int sessionId, CancellationToken stopping)
    {
        ArgumentNullException.ThrowIfNull(network);
        using var login = CancellationTokenSource.CreateLinkedTokenSource(stopping);
        login.CancelAfter(_options.LoginTimeout);

        var handshake = new TdsServerHandshake(_options.Certificate, _options.Identity.Version, _logger);
        IGenericResult<SslStream> secured;
        try
        {
            secured = await handshake.Secure(network, login.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (login.IsCancellationRequested)
        {
            return "the handshake did not finish in time";
        }

        if (secured.IsFailure)
            return "the handshake failed";

        await using var tls = secured.Value!;
        var writer = new TdsMessageWriter(tls, _logger) { SessionId = (ushort)sessionId };

        // Why a reader of its own: the login comes from someone not yet signed in, so it is bounded by what a
        // LOGIN7 can need, not by the larger limit a signed-in caller's batches get.
        var loginMessages = new TdsMessageReader(tls, Math.Min(Login7Request.LargestPayloadBytes, _options.MaximumMessageBytes), _logger);
        var session = await SignIn(loginMessages, writer, sessionId, login.Token).ConfigureAwait(false);
        if (session.IsFailure)
            return "the login did not complete";

        try
        {
            return await Requests(new TdsMessageReader(tls, _options.MaximumMessageBytes, _logger), writer, session.Value!, stopping).ConfigureAwait(false);
        }
        finally
        {
            await CloseSession(session.Value!, stopping).ConfigureAwait(false);
        }
    }

    // The platform session ends with the connection. A server that is stopping may not get to close it; it then ends
    // at its own expiry, which is logged rather than raised as a fault of the connection.
    private async Task CloseSession(TdsSession session, CancellationToken stopping)
    {
        try
        {
            await using var scope = _scopes.CreateAsyncScope();
            var closed = await TdsSessionClosing.Close(
                scope.ServiceProvider.GetRequiredService<ISessionStore>(), session.SessionToken, session.SessionServiceName, stopping).ConfigureAwait(false);
            if (closed.IsSuccess)
                TdsEndpointLog.SessionClosed(_logger, session.SessionId);
            else
                TdsEndpointLog.SessionNotClosed(_logger, session.SessionId);
        }
        catch (OperationCanceledException) when (stopping.IsCancellationRequested)
        {
            TdsEndpointLog.SessionNotClosed(_logger, session.SessionId);
        }
    }

    private async Task<IGenericResult<TdsSession>> SignIn(TdsMessageReader reader, TdsMessageWriter writer, int sessionId, CancellationToken cancellationToken)
    {
        try
        {
            var message = await reader.Read(cancellationToken).ConfigureAwait(false);
            if (message.IsFailure)
                return message.ToNewResult<TdsSession>();

            if (message.Value!.PacketType != TdsPacketTypes.Login7)
                return GenericResult<TdsSession>.Failure(TdsEndpointLog.NotSupported(_logger, $"a connection whose login is a 0x{message.Value.PacketType:X2} message"));

            var login = Login7Request.Parse(message.Value.Payload, _logger);
            if (login.IsFailure)
                return login.ToNewResult<TdsSession>();

            return await Answer(login.Value!, writer, sessionId, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return GenericResult<TdsSession>.Failure(TdsEndpointLog.NotSupported(_logger, "a login that did not finish in time"));
        }
    }

    private async Task<IGenericResult<TdsSession>> Answer(Login7Request login, TdsMessageWriter writer, int sessionId, CancellationToken cancellationToken)
    {
        TdsLoginOutcome outcome;
        await using (var scope = _scopes.CreateAsyncScope())
        {
            outcome = await new TdsLogin(_options, _logger).SignIn(login, scope.ServiceProvider, sessionId, cancellationToken).ConfigureAwait(false);
        }

        var payload = new TdsPayloadWriter(writer.PacketSize);
        if (outcome.Session is null)
        {
            foreach (var error in outcome.Errors)
                TdsTokenWriter.Message(payload, error, _options.Identity.ServerName);

            TdsTokenWriter.Done(payload, TdsTokenTypes.Done, TdsDoneStatus.Error, 0, 0);
            await writer.Write(TdsPacketTypes.TabularResult, payload.Written, cancellationToken).ConfigureAwait(false);
            return GenericResult<TdsSession>.Failure(TdsEndpointLog.SessionEnded(_logger, sessionId));
        }

        // The packet size agreed takes effect after the login response, which travels at the size in force.
        var packetSize = Math.Clamp(login.PacketSize, TdsMessageWriter.SmallestPacketSize, TdsMessageWriter.LargestPacketSize);
        TdsLoginResponse.Write(payload, outcome.Session, _options.Identity, outcome.TdsVersion, packetSize, login.HasFeatureExtension);
        var sent = await writer.Write(TdsPacketTypes.TabularResult, payload.Written, cancellationToken).ConfigureAwait(false);
        if (sent.IsFailure)
            return sent.ToNewResult<TdsSession>();

        writer.PacketSize = packetSize;
        return GenericResult<TdsSession>.Success(outcome.Session);
    }

    private async Task<string> Requests(TdsMessageReader reader, TdsMessageWriter writer, TdsSession session, CancellationToken stopping)
    {
        var gate = new AttentionGate();
        // Why two: a client without MARS waits for each answer, so at most one request waits behind the one
        // running, with an attention after it. A client that keeps sending is held at the socket, not in memory.
        var inbox = Channel.CreateBounded<TdsMessage>(new BoundedChannelOptions(2)
        {
            SingleReader = true,
            SingleWriter = true,
            FullMode = BoundedChannelFullMode.Wait,
        });
        using var closing = CancellationTokenSource.CreateLinkedTokenSource(stopping);
        var reading = Read(reader, inbox.Writer, gate, closing.Token);
        string reason;
        try
        {
            reason = await Answer(inbox.Reader, writer, session, gate, closing.Token).ConfigureAwait(false);
        }
        finally
        {
            await closing.CancelAsync().ConfigureAwait(false);
        }

        var readerReason = await reading.ConfigureAwait(false);
        return reason.Length > 0 ? reason : readerReason;
    }

    // Reads every message the client sends. An attention cancels the request it interrupts; one that finds
    // none running goes in line, to be acknowledged on its own.
    private static async Task<string> Read(TdsMessageReader reader, ChannelWriter<TdsMessage> inbox, AttentionGate gate, CancellationToken closing)
    {
        try
        {
            while (true)
            {
                var message = await reader.Read(closing).ConfigureAwait(false);
                if (message.IsFailure)
                    return "a message could not be read";

                if (ReferenceEquals(message.Value, TdsMessage.Closed))
                    return "the client closed the connection";

                if (message.Value!.PacketType == TdsPacketTypes.Attention && gate.Signal())
                    continue;

                await inbox.WriteAsync(message.Value, closing).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (closing.IsCancellationRequested)
        {
            return "the connection was closed";
        }
        finally
        {
            inbox.TryComplete();
        }
    }

    private async Task<string> Answer(ChannelReader<TdsMessage> inbox, TdsMessageWriter writer, TdsSession session, AttentionGate gate, CancellationToken closing)
    {
        try
        {
            await foreach (var message in inbox.ReadAllAsync(closing).ConfigureAwait(false))
            {
                var answered = message.PacketType == TdsPacketTypes.Attention
                    ? await Acknowledge(writer, closing).ConfigureAwait(false)
                    : await AnswerRequest(message, writer, session, gate, closing).ConfigureAwait(false);
                if (answered.IsFailure)
                    return "a request could not be answered";
            }

            return string.Empty;
        }
        catch (OperationCanceledException) when (closing.IsCancellationRequested)
        {
            return "the server is stopping";
        }
    }

    private async Task<IGenericResult> AnswerRequest(TdsMessage message, TdsMessageWriter writer, TdsSession session, AttentionGate gate, CancellationToken closing)
    {
        using var request = CancellationTokenSource.CreateLinkedTokenSource(closing);
        gate.Begin(request);
        IGenericResult<bool> answered;
        bool attended;
        try
        {
            answered = await _dispatcher.Answer(message, session, writer, request.Token, closing).ConfigureAwait(false);
        }
        finally
        {
            attended = gate.End();
        }

        if (answered.IsFailure)
            return answered;

        // An attention that arrived after the request's last cancellation check still needs its acknowledgement.
        return attended && !answered.Value
            ? await Acknowledge(writer, closing).ConfigureAwait(false)
            : GenericResult.Success();
    }

    private static async Task<IGenericResult> Acknowledge(TdsMessageWriter writer, CancellationToken closing)
    {
        var payload = new TdsPayloadWriter(16);
        TdsTokenWriter.Done(payload, TdsTokenTypes.Done, TdsDoneStatus.Attention, 0, 0);
        return await writer.Write(TdsPacketTypes.TabularResult, payload.Written, closing).ConfigureAwait(false);
    }
}
