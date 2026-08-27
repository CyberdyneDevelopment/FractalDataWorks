using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Context;
using Fdw.Services.Authentication.Abstractions.Steps;
using Fdw.Services.Authentication.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Fdw.Services.Authentication.Steps;

/// <summary>
/// Sends a caller to an OIDC provider and turns what comes back into a subject.
/// </summary>
/// <remarks>
/// <para>
/// Runs twice. On the first call it has no authorization code, so it returns a challenge: the
/// destination and a single-use resume token the runner stores alongside everything established so
/// far. When the caller returns, the callback resumes the flow, this step exchanges the code, and
/// verifies the token it gets back.
/// </para>
/// <para>
/// Authorization code with PKCE, always — <span>RFC 9700</span> requires it of confidential clients
/// too, not only public ones. The implicit grant this replaces is prohibited outright.
/// </para>
/// </remarks>
public sealed class OidcRedirectStep : IAuthenticationStep
{
    private readonly OidcRedirectStepConfiguration _configuration;
    private readonly ISigningKeyProvider _keys;
    private readonly IOidcCallbackAccessor _callback;
    private readonly IAuthorizationRequestStore _requests;
    private readonly HttpClient _http;
    private readonly ILogger<OidcRedirectStep> _logger;

    /// <summary>Initializes a new instance of the <see cref="OidcRedirectStep"/> class.</summary>
    /// <param name="configuration">Which provider, and on what terms.</param>
    /// <param name="keys">Supplies the provider's signing keys.</param>
    /// <param name="callback">Supplies the code and state when the caller returns.</param>
    /// <param name="requests">Holds the verifier and nonce across the redirect.</param>
    /// <param name="http">Calls the token endpoint.</param>
    /// <param name="logger">The logger.</param>
    public OidcRedirectStep(
        OidcRedirectStepConfiguration configuration,
        ISigningKeyProvider keys,
        IOidcCallbackAccessor callback,
        IAuthorizationRequestStore requests,
        HttpClient http,
        ILogger<OidcRedirectStep>? logger = null)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _keys = keys ?? throw new ArgumentNullException(nameof(keys));
        _callback = callback ?? throw new ArgumentNullException(nameof(callback));
        _requests = requests ?? throw new ArgumentNullException(nameof(requests));
        _http = http ?? throw new ArgumentNullException(nameof(http));
        _logger = logger ?? NullLogger<OidcRedirectStep>.Instance;
    }

    /// <inheritdoc />
    public IReadOnlyList<ContextElement> Requires => [];

    /// <inheritdoc />
    public IReadOnlyList<ContextElement> Contributes => [ContextElement.Subject, ContextElement.Claims];

    /// <inheritdoc />
    public string? AuthenticationMethod => _configuration.AuthenticationMethod;

    /// <inheritdoc />
    public async Task<IGenericResult<StepOutcome>> Execute(
        AuthenticationContext context, CancellationToken cancellationToken = default)
        => _callback.Code is { Length: > 0 } code
            ? await Complete(code, cancellationToken).ConfigureAwait(false)
            : Begin();

    private IGenericResult<StepOutcome> Begin()
    {
        var verifier = Base64Url(RandomNumberGenerator.GetBytes(32));
        var state = Base64Url(RandomNumberGenerator.GetBytes(32));
        var nonce = Base64Url(RandomNumberGenerator.GetBytes(32));

        var stored = _requests.Store(state, new AuthorizationRequest
        {
            CodeVerifier = verifier,
            Nonce = nonce,
            Issuer = _configuration.Issuer,
        });

        if (stored.IsFailure)
            return stored.ToNewResult<StepOutcome>();

        // Why S256 and not plain: the plain method sends the verifier itself, so anyone who can see
        // the authorization request can complete the exchange. RFC 7636 permits plain; RFC 9700
        // does not.
        var challenge = Base64Url(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));

        var destination = new UriBuilder(_configuration.AuthorizationEndpoint)
        {
            Query = string.Join('&',
            [
                "response_type=code",
                $"client_id={Uri.EscapeDataString(_configuration.ClientId)}",
                $"redirect_uri={Uri.EscapeDataString(_configuration.RedirectUri.ToString())}",
                $"scope={Uri.EscapeDataString(string.Join(' ', _configuration.Scopes))}",
                $"state={Uri.EscapeDataString(state)}",
                $"nonce={Uri.EscapeDataString(nonce)}",
                $"code_challenge={Uri.EscapeDataString(challenge)}",
                "code_challenge_method=S256",
            ]),
        }.Uri;

        OidcLog.Challenging(_logger, _configuration.Issuer);

        return GenericResult<StepOutcome>.Success(new StepOutcome.Challenge(destination, state));
    }

    private async Task<IGenericResult<StepOutcome>> Complete(string code, CancellationToken cancellationToken)
    {
        if (_callback.State is not { Length: > 0 } state)
            return GenericResult<StepOutcome>.Failure(OidcLog.StateMissing(_logger, _configuration.Issuer));

        // Why consumed rather than read: state is single-use. A returned authorization code that can
        // be presented twice is a code that can be replayed.
        var request = _requests.TryConsume(state);
        if (request.IsFailure)
            return request.ToNewResult<StepOutcome>();

        // Why the issuer is checked here: with several providers configured, a code from one could
        // otherwise be exchanged at another's token endpoint. RFC 9207 exists for this.
        if (!string.Equals(request.Value!.Issuer, _configuration.Issuer, StringComparison.Ordinal))
            return GenericResult<StepOutcome>.Failure(
                OidcLog.IssuerMismatch(_logger, _configuration.Issuer, request.Value!.Issuer));

        var exchanged = await Exchange(code, request.Value!.CodeVerifier, cancellationToken).ConfigureAwait(false);
        if (exchanged.IsFailure)
            return exchanged.ToNewResult<StepOutcome>();

        var keys = await _keys.Current(_configuration.JwksUri, cancellationToken).ConfigureAwait(false);
        if (keys.IsFailure)
            return keys.ToNewResult<StepOutcome>();

        var validated = await new JsonWebTokenHandler().ValidateTokenAsync(exchanged.Value!,
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = _configuration.Issuer,
                ValidateAudience = true,
                ValidAudiences = _configuration.ValidAudiences,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKeys = keys.Value,
                ValidAlgorithms = _configuration.ValidAlgorithms,
                ClockSkew = _configuration.ClockSkew,
            }).ConfigureAwait(false);

        if (!validated.IsValid)
            return GenericResult<StepOutcome>.Failure(OidcLog.TokenRejected(
                _logger, _configuration.Issuer, validated.Exception?.GetType().Name ?? "unknown"));

        // Why the nonce is checked and not merely sent: it binds the token to the request this
        // platform made. Without the check, a token minted for a different session replays here.
        var nonce = validated.ClaimsIdentity.FindFirst("nonce")?.Value;
        if (!string.Equals(nonce, request.Value!.Nonce, StringComparison.Ordinal))
            return GenericResult<StepOutcome>.Failure(OidcLog.NonceMismatch(_logger, _configuration.Issuer));

        var subjectId = validated.ClaimsIdentity.FindFirst(_configuration.SubjectClaim)?.Value;
        if (string.IsNullOrWhiteSpace(subjectId))
            return GenericResult<StepOutcome>.Failure(
                OidcLog.SubjectClaimMissing(_logger, _configuration.Issuer, _configuration.SubjectClaim));

        OidcLog.Completed(_logger, _configuration.Issuer);

        return GenericResult<StepOutcome>.Success(new StepOutcome.Contributed(new ContextContribution
        {
            Subject = new Subject
            {
                Issuer = _configuration.Issuer,
                SubjectId = subjectId,
                AuthenticatedAt = DateTimeOffset.UtcNow,
            },
            Claims = [.. validated.ClaimsIdentity.Claims
                .Where(c => c.Type is not ("sub" or "aud" or "iss" or "exp" or "nbf" or "iat" or "nonce"))
                .Select(c => new Claim
                {
                    Type = c.Type,
                    Value = c.Value,
                    Source = ClaimSource.External,
                    Issuer = _configuration.Issuer,
                })],
        }));
    }

    private async Task<IGenericResult<string>> Exchange(
        string code, string verifier, CancellationToken cancellationToken)
    {
        var form = new List<KeyValuePair<string, string>>
        {
            new("grant_type", "authorization_code"),
            new("code", code),
            new("redirect_uri", _configuration.RedirectUri.ToString()),
            new("client_id", _configuration.ClientId),
            new("code_verifier", verifier),
        };

        try
        {
            using var response = await _http
                .PostAsync(_configuration.TokenEndpoint, new FormUrlEncodedContent(form), cancellationToken)
                .ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
                return GenericResult<string>.Failure(OidcLog.ExchangeRefused(
                    _logger, _configuration.Issuer,
                    ((int)response.StatusCode).ToString(CultureInfo.InvariantCulture)));

            using var document = JsonDocument.Parse(
                await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false));

            return document.RootElement.TryGetProperty("id_token", out var token)
                && token.GetString() is { Length: > 0 } value
                ? GenericResult<string>.Success(value)
                : GenericResult<string>.Failure(OidcLog.NoIdToken(_logger, _configuration.Issuer));
        }
        catch (HttpRequestException ex)
        {
            return GenericResult<string>.Failure(
                OidcLog.ExchangeFailed(_logger, _configuration.Issuer, ex.GetType().Name));
        }
    }

    private static string Base64Url(byte[] bytes)
        => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
