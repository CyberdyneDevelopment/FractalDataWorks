using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Aegis.Abstractions;
using Fdw.Aegis.Logging;
using Fdw.Results;
using Fdw.Services.SecretManagers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Aegis.Targets;

/// <summary>
/// The Phase 1 <see cref="IAegisInjectionTarget"/>: injects the resolved secret as an outbound HTTP
/// <c>Authorization: Bearer</c> header. Generalizes <c>ConfiguredMcpClientDelegate.CreateTransport</c>'s
/// <c>env[TargetEnvironmentVariable]=token</c> injection (mcp-hub) from a spawned process's
/// environment to an HTTP request header.
/// </summary>
/// <remarks>
/// Why a raw <see cref="IHttpClientFactory"/> here is in-pattern (not a DataGateway violation): this
/// is the brokered downstream ACTION Aegis exists to gate — the egress point — exactly like
/// <c>ConfiguredMcpClientDelegate</c>, which also does not route through <c>IDataGateway</c>. The
/// DataGateway-only rule governs FDW data access, not an arbitrary downstream HTTP call this
/// injector is approved to make on Claude's behalf.
/// </remarks>
public sealed class HttpHeaderInjectionTarget : IAegisInjectionTarget
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<HttpHeaderInjectionTarget> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpHeaderInjectionTarget"/> class.
    /// </summary>
    public HttpHeaderInjectionTarget(IHttpClientFactory httpClientFactory, ILogger<HttpHeaderInjectionTarget>? logger = null)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _logger = logger ?? NullLogger<HttpHeaderInjectionTarget>.Instance;
    }

    /// <inheritdoc />
    public async Task<IGenericResult<AegisInjectionOutcome>> Inject(
        ApprovalRequest request, SecretValue secret, CancellationToken cancellationToken = default)
    {
        // Why: a NAMED HttpClient keyed by ConnectionName — the client's BaseAddress (the downstream
        // endpoint) is configured once at DI-registration time (Program.cs, keyed off the same
        // ConnectionName every declared Commands entry names). Inject() never needs to know the URL
        // itself, only which pre-configured client to pick — the standard IHttpClientFactory
        // named-client pattern, no bespoke endpoint-resolution mechanism.
        var client = _httpClientFactory.CreateClient(request.ConnectionName);

        // Why validate BEFORE the header is constructed: AuthenticationHeaderValue's validator (and
        // the send-time header serializer) throw a FormatException whose message EMBEDS the offending
        // value — which must never reach a log or the caller. A secret carrying CR/LF would also be an
        // outbound header-injection vector. Rejecting here fails loud with a message that can never
        // contain the secret, and closes both holes at once.
        var token = secret.GetStringValue();
        if (!IsValidHeaderValue(token))
            return GenericResult<AegisInjectionOutcome>.Failure(
                AegisLog.InjectionFailed(_logger, request.CommandName, "resolved secret is not a valid HTTP header value"));

        try
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Get, string.Empty);
            // Why: the plaintext lives only as this local, passed straight into the header value —
            // never assigned to a field, never logged, never captured by a closure that outlives
            // this call.
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var response = await client.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false);
            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                return GenericResult<AegisInjectionOutcome>.Failure(
                    AegisLog.InjectionFailed(_logger, request.CommandName, $"downstream returned {(int)response.StatusCode}"));
            }

            // Why NEVER return the body verbatim: a hostile or merely verbose downstream can reflect
            // the credential back in its response body (or a 401 error string), and Reference crosses
            // the boundary into Claude's context. The sanitized receipt is the status code plus a
            // non-reversible fingerprint of the body — proof a response arrived, with none of its
            // content surfaced. Any future need for real downstream data must flow through a
            // command-DECLARED response projection, never a raw pass-through.
            return GenericResult<AegisInjectionOutcome>.Success(new AegisInjectionOutcome
            {
                Success = true,
                CorrelationId = request.CorrelationId,
                Reference = $"status={(int)response.StatusCode};body={Fingerprint(body)}",
            });
        }
        catch (OperationCanceledException)
        {
            // Why: cancellation is not an injection failure — propagate it, never wrap it in a
            // sanitized-looking failure result.
            throw;
        }
        catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException)
        {
            // Why safe to surface ex.Message here: a transport error (DNS/connect/TLS) never contains
            // the token — the token only ever lived in a request header, not the exception text — and
            // the value-echoing header-format exception is pre-empted by IsValidHeaderValue above.
            return GenericResult<AegisInjectionOutcome>.Failure(AegisLog.InjectionFailed(_logger, request.CommandName, ex.Message));
        }
    }

    // Why: an HTTP field-value must not contain CR, LF, NUL, or other control characters. Rejecting
    // them prevents outbound header injection AND the AuthenticationHeaderValue FormatException whose
    // message would embed the secret. Legitimate bearer tokens (base64url / hex / JWT) contain no
    // control characters, so this never rejects a valid credential.
    private static bool IsValidHeaderValue(string value)
    {
        if (string.IsNullOrEmpty(value))
            return false;

        foreach (var c in value)
        {
            if (char.IsControl(c))
                return false;
        }

        return true;
    }

    // Why: a non-reversible fingerprint of the downstream body — a stable receipt that proves a
    // response was received without ever surfacing its (possibly secret-reflecting) content.
    private static string Fingerprint(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)))[..12].ToLowerInvariant();
}
