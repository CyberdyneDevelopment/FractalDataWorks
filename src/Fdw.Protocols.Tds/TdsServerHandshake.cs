using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Protocols.Tds.Logging;
using Fdw.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Protocols.Tds;

/// <summary>
/// Takes a new connection from its first byte to an encrypted stream that carries TDS messages: the
/// pre-login exchange and the TLS handshake, in whichever order the client opens with.
/// </summary>
/// <remarks>
/// <para>
/// A TDS 8 (strict) client opens with TLS and sends its pre-login inside it. A TDS 7 client sends its
/// pre-login in the clear, and the TLS handshake then runs inside pre-login packets. The first byte tells
/// them apart, and both end at the same place: a TLS stream every later message travels in.
/// </para>
/// <para>
/// The whole connection is always encrypted. TDS 7 also allows encrypting only the login packet; this
/// server answers every client so that it encrypts everything, and refuses one that cannot encrypt at all,
/// because the login carries a password that pre-login obfuscation does not protect.
/// </para>
/// </remarks>
public sealed class TdsServerHandshake
{
    private static readonly SslApplicationProtocol Tds8 = new("tds/8.0");

    // TDS 7 carries the TLS handshake inside pre-login packets, and SQL Server negotiates TLS 1.2 there. A driver
    // stops wrapping once its own side of the handshake is done, which under TLS 1.3 comes before its last flight:
    // ODBC Driver 18 sends that ChangeCipherSpec and Finished bare, and the handshake fails. TLS 1.3 belongs to
    // TDS 8, where TLS comes first and nothing is wrapped.
    private const SslProtocols PreLoginProtocols = SslProtocols.Tls12;
    private const SslProtocols StrictProtocols = SslProtocols.Tls12 | SslProtocols.Tls13;

    private readonly X509Certificate2 _certificate;
    private readonly TdsServerVersion _version;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="TdsServerHandshake"/> class.</summary>
    /// <param name="certificate">The certificate this server proves its name with.</param>
    /// <param name="version">The version this server reports in pre-login.</param>
    /// <param name="logger">The logger.</param>
    public TdsServerHandshake(X509Certificate2 certificate, TdsServerVersion version, ILogger? logger)
    {
        _certificate = certificate ?? throw new ArgumentNullException(nameof(certificate));
        _version = version ?? throw new ArgumentNullException(nameof(version));
        _logger = logger ?? NullLogger.Instance;
    }

    /// <summary>Runs the handshake on a new connection.</summary>
    /// <param name="network">The connection's stream; the caller keeps ownership of it.</param>
    /// <param name="cancellationToken">A token to cancel the handshake.</param>
    /// <returns>The encrypted stream every later message travels in, or the refusal.</returns>
    public async Task<IGenericResult<SslStream>> Secure(Stream network, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(network);

        var first = await FirstByte(network, cancellationToken).ConfigureAwait(false);
        if (first.IsFailure)
            return first.ToNewResult<SslStream>();

        if (first.Value == TdsPacketTypes.TlsHandshakeRecord)
            return await Strict(new ReplayPrefixStream(network, [first.Value]), cancellationToken).ConfigureAwait(false);

        if (first.Value == TdsPacketTypes.PreLogin)
            return await PreLoginThenTls(new ReplayPrefixStream(network, [first.Value]), cancellationToken).ConfigureAwait(false);

        return GenericResult<SslStream>.Failure(TdsProtocolLog.Malformed(_logger, "connection",
            $"it opened with 0x{first.Value:X2}, which is neither a pre-login packet nor a TLS record"));
    }

    // TDS 8: TLS first, then the pre-login exchange inside it. The TLS stream owns the replay stream
    // from here, so disposing it on a refusal disposes both.
    private async Task<IGenericResult<SslStream>> Strict(ReplayPrefixStream replay, CancellationToken cancellationToken)
    {
        var tls = new SslStream(replay, leaveInnerStreamOpen: false);
        var secured = await Authenticate(tls, [Tds8], strict: true, cancellationToken).ConfigureAwait(false);
        var answered = secured.IsSuccess
            ? await AnswerPreLogin(tls, strictlyEncrypted: true, cancellationToken).ConfigureAwait(false)
            : secured;

        if (answered.IsSuccess)
            return GenericResult<SslStream>.Success(tls);

        await tls.DisposeAsync().ConfigureAwait(false);
        return answered.ToNewResult<SslStream>();
    }

    // TDS 7: the pre-login exchange in the clear, then TLS inside pre-login packets.
    private async Task<IGenericResult<SslStream>> PreLoginThenTls(ReplayPrefixStream replay, CancellationToken cancellationToken)
    {
        var answered = await AnswerPreLogin(replay, strictlyEncrypted: false, cancellationToken).ConfigureAwait(false);
        if (answered.IsFailure)
        {
            await replay.DisposeAsync().ConfigureAwait(false);
            return answered.ToNewResult<SslStream>();
        }

        var wrapped = new TdsTlsHandshakeStream(replay);
        var tls = new SslStream(wrapped, leaveInnerStreamOpen: false);
        var secured = await Authenticate(tls, [], strict: false, cancellationToken).ConfigureAwait(false);
        if (secured.IsFailure)
        {
            await tls.DisposeAsync().ConfigureAwait(false);
            return secured.ToNewResult<SslStream>();
        }

        wrapped.EndHandshake();
        return GenericResult<SslStream>.Success(tls);
    }

    // Why one packet: a pre-login is a handful of options a few dozen bytes long, sent before anyone has signed in.
    // Reading it against the signed-in message limit would let a stranger make the server buffer megabytes.
    private async Task<IGenericResult> AnswerPreLogin(Stream stream, bool strictlyEncrypted, CancellationToken cancellationToken)
    {
        var message = await new TdsMessageReader(stream, TdsMessageReader.SmallestLimit, _logger).Read(cancellationToken).ConfigureAwait(false);
        if (message.IsFailure)
            return message;

        if (message.Value!.PacketType != TdsPacketTypes.PreLogin)
            return GenericResult.Failure(TdsProtocolLog.Malformed(_logger, "connection",
                $"its first message is of type 0x{message.Value.PacketType:X2}, not pre-login"));

        var request = PreLoginRequest.Parse(message.Value.Payload, _logger);
        if (request.IsFailure)
            return request;

        // Inside TLS already: whatever the client offered, the connection is encrypted.
        var answer = strictlyEncrypted ? TdsEncryptionModes.On : TdsEncryptionModes.Answer(request.Value!.Encryption);
        TdsProtocolLog.PreLoginNegotiated(_logger, request.Value!.Encryption, answer);

        var written = await new TdsMessageWriter(stream, _logger)
            .Write(TdsPacketTypes.TabularResult, PreLoginResponse.Build(_version, answer, request.Value.AsksForFederatedAuthentication), cancellationToken)
            .ConfigureAwait(false);
        if (written.IsFailure)
            return written;

        return answer == TdsEncryptionModes.NotSupported
            ? GenericResult.Failure(TdsProtocolLog.NotSupported(_logger, "a connection without encryption"))
            : GenericResult.Success();
    }

    private async Task<IGenericResult> Authenticate(
        SslStream tls, List<SslApplicationProtocol> protocols, bool strict, CancellationToken cancellationToken)
    {
        var options = new SslServerAuthenticationOptions
        {
            ServerCertificate = _certificate,
            ClientCertificateRequired = false,
            ApplicationProtocols = protocols.Count == 0 ? null : protocols,
            EnabledSslProtocols = strict ? StrictProtocols : PreLoginProtocols,
        };

        try
        {
            await tls.AuthenticateAsServerAsync(options, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is AuthenticationException or IOException)
        {
            return GenericResult.Failure(TdsProtocolLog.TransportFailed(_logger, ex, "negotiating TLS"));
        }

        TdsProtocolLog.TlsEstablished(_logger, tls.SslProtocol.ToString(), strict);
        return GenericResult.Success();
    }

    private async Task<IGenericResult<byte>> FirstByte(Stream network, CancellationToken cancellationToken)
    {
        var buffer = new byte[1];
        try
        {
            var read = await network.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
            return read == 1
                ? GenericResult<byte>.Success(buffer[0])
                : GenericResult<byte>.Failure(TdsProtocolLog.Malformed(_logger, "connection", "it closed before sending anything"));
        }
        catch (IOException ex)
        {
            return GenericResult<byte>.Failure(TdsProtocolLog.TransportFailed(_logger, ex, "reading the first byte"));
        }
    }
}
