using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Identity.Abstractions;
using Fdw.Services.Identity.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Identity;

/// <summary>
/// Posts an OAuth 2.0 client-credentials token request to a provider's token endpoint and reads the
/// issued token back.
/// </summary>
/// <remarks>
/// <para>
/// Every identity mechanism uses <c>grant_type=client_credentials</c> (RFC 6749 §4.4) and differs
/// only in how the request authenticates itself — a <c>client_secret</c> for service-account
/// mechanisms, a <c>client_assertion</c> for federated-JWT ones. That difference is the caller's
/// contribution to the form; everything after it (transport, status handling, response parsing,
/// expiry computation) is identical and lives here once, for every provider. Nothing in this class
/// is specific to one identity provider — it is the OAuth 2.0 exchange itself.
/// </para>
/// <para>
/// Nothing in this class logs a credential or a token value. Failures report the endpoint, the
/// status, and the provider's own error text, which is what diagnoses a misconfiguration without
/// handing a reader the ability to impersonate the service.
/// </para>
/// </remarks>
public sealed class OAuth2TokenEndpointClient
{
    private readonly HttpClient _http;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="OAuth2TokenEndpointClient"/> class.</summary>
    /// <param name="http">The HTTP client used to reach the token endpoint.</param>
    /// <param name="logger">The logger for this client.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="http"/> is null.</exception>
    public OAuth2TokenEndpointClient(HttpClient http, ILogger? logger = null)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
        _logger = logger ?? NullLogger.Instance;
    }

    /// <summary>
    /// Exchanges <paramref name="credentialForm"/> at <paramref name="tokenEndpoint"/> for a token
    /// valid at the audience named in <paramref name="request"/>.
    /// </summary>
    /// <param name="configurationName">The identity configuration performing the exchange, for logging.</param>
    /// <param name="tokenEndpoint">The absolute token endpoint URL.</param>
    /// <param name="issuer">The issuer to stamp on the resulting token.</param>
    /// <param name="request">The audience and scopes being asked for.</param>
    /// <param name="credentialForm">The grant-specific parameters that authenticate this request.</param>
    /// <param name="cancellationToken">Propagated cancellation token.</param>
    /// <returns>The issued token, or the structured reason the exchange did not produce one.</returns>
    public async Task<IGenericResult<IssuedIdentityToken>> Exchange(
        string configurationName,
        string tokenEndpoint,
        string issuer,
        IdentityTokenRequest request,
        IReadOnlyDictionary<string, string> credentialForm,
        CancellationToken cancellationToken = default)
    {
        var form = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["grant_type"] = "client_credentials",
        };

        foreach (var parameter in credentialForm)
            form[parameter.Key] = parameter.Value;

        form["audience"] = request.Audience;

        if (request.Scopes.Count > 0)
            form["scope"] = string.Join(" ", request.Scopes);

        // Trace names the parameters but never their values — client_secret and client_assertion are
        // credentials, and a trace log that carried them would defeat the point of short-lived tokens.
        IdentityLog.PostingTokenRequest(_logger, configurationName, tokenEndpoint, "client_credentials", string.Join(", ", form.Keys));

        HttpResponseMessage response;
        try
        {
            response = await _http.PostAsync(
                new Uri(tokenEndpoint),
                new FormUrlEncodedContent(form),
                cancellationToken).ConfigureAwait(false);
        }
        catch (HttpRequestException ex)
        {
            return GenericResult<IssuedIdentityToken>.Failure(
                IdentityLog.ProviderUnreachable(_logger, ex, configurationName, tokenEndpoint));
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            return GenericResult<IssuedIdentityToken>.Failure(
                IdentityLog.ProviderUnreachable(_logger, ex, configurationName, tokenEndpoint));
        }

        using (response)
        {
            IdentityLog.TokenEndpointAnswered(_logger, configurationName, tokenEndpoint, (int)response.StatusCode);
            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                return GenericResult<IssuedIdentityToken>.Failure(
                    response.StatusCode is System.Net.HttpStatusCode.BadRequest or System.Net.HttpStatusCode.Unauthorized
                        ? IdentityLog.CredentialRejected(_logger, configurationName, issuer, DescribeError(configurationName, body))
                        : IdentityLog.ProviderReturnedError(_logger, configurationName, issuer, (int)response.StatusCode));
            }

            return Read(configurationName, issuer, request, body);
        }
    }

    private IGenericResult<IssuedIdentityToken> Read(
        string configurationName,
        string issuer,
        IdentityTokenRequest request,
        string body)
    {
        JsonElement root;
        try
        {
            root = JsonDocument.Parse(body).RootElement;
        }
        catch (JsonException ex)
        {
            return GenericResult<IssuedIdentityToken>.Failure(
                IdentityLog.TokenResponseUnreadable(_logger, ex, configurationName, issuer));
        }

        if (!root.TryGetProperty("access_token", out var accessToken) || accessToken.GetString() is not { Length: > 0 } tokenValue)
            return GenericResult<IssuedIdentityToken>.Failure(
                IdentityLog.TokenResponseIncomplete(_logger, configurationName, issuer, "access_token"));

        if (!root.TryGetProperty("expires_in", out var expiresIn) || !expiresIn.TryGetInt32(out var lifetimeSeconds))
            return GenericResult<IssuedIdentityToken>.Failure(
                IdentityLog.TokenResponseIncomplete(_logger, configurationName, issuer, "expires_in"));

        if (!root.TryGetProperty("token_type", out var tokenType) || tokenType.GetString() is not { Length: > 0 } tokenTypeValue)
            return GenericResult<IssuedIdentityToken>.Failure(
                IdentityLog.TokenResponseIncomplete(_logger, configurationName, issuer, "token_type"));

        var granted = root.TryGetProperty("scope", out var scope) && scope.GetString() is { Length: > 0 } scopeValue
            ? scopeValue.Split([' '], StringSplitOptions.RemoveEmptyEntries)
            : [];

        if (request.Scopes.Count > 0 && granted.Length > 0 && request.Scopes.Any(asked => !granted.Contains(asked, StringComparer.Ordinal)))
            IdentityLog.ScopesNarrowed(_logger, configurationName, string.Join(" ", request.Scopes), string.Join(" ", granted));

        var issued = new IssuedIdentityToken(
            tokenValue,
            tokenTypeValue,
            issuer,
            request.Audience,
            DateTimeOffset.UtcNow.AddSeconds(lifetimeSeconds),
            granted);

        IdentityLog.TokenIssued(_logger, configurationName, issuer, request.Audience, string.Join(" ", granted), issued.ExpiresAt);
        return GenericResult<IssuedIdentityToken>.Success(issued);
    }

    private string DescribeError(string configurationName, string body)
    {
        try
        {
            var root = JsonDocument.Parse(body).RootElement;
            return root.TryGetProperty("error_description", out var description) && description.GetString() is { Length: > 0 } detail
                ? detail
                : root.TryGetProperty("error", out var error) && error.GetString() is { Length: > 0 } code
                    ? code
                    : "no error detail in response";
        }
        catch (JsonException ex)
        {
            IdentityLog.ErrorResponseUnparseable(_logger, ex, configurationName);
            return "unparseable error response";
        }
    }
}
