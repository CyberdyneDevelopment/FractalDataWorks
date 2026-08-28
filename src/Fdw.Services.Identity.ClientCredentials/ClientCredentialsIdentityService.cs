using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Identity.Abstractions;
using Fdw.Services.Identity.Logging;
using Fdw.ServiceTypes;
using Fdw.Services.SecretManagers;
using Fdw.Services.SecretManagers.Abstractions;
using Fdw.Services.SecretManagers.Commands;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Identity.ClientCredentials;

/// <summary>
/// Proves this service's identity with an OAuth 2.0 client-credentials grant (RFC 6749 §4.4) against
/// any conforming token endpoint. FDW's own OpenIddict authorization server is the usual target.
/// </summary>
/// <remarks>
/// The client secret is resolved through <c>ISecretManager</c> at acquisition time and never held on
/// this instance. Holding it in a field would keep a long-lived credential in process memory for the
/// life of the service, which is a worse exposure than the per-acquisition read it replaces.
/// </remarks>
public sealed class ClientCredentialsIdentityService
    : IdentityServiceBase<ClientCredentialsConfiguration, ClientCredentialsIdentityService>
{
    private readonly OAuth2TokenEndpointClient _tokenEndpoint;
    private readonly Lazy<ISecretManagerProvider> _secretManagers;

    /// <summary>
    /// Initializes a new instance of the <see cref="ClientCredentialsIdentityService"/> class.
    /// </summary>
    /// <param name="logger">The logger for this service.</param>
    /// <param name="configuration">The typed configuration body for this identity.</param>
    /// <param name="tokenEndpoint">The shared token-endpoint client.</param>
    /// <param name="secretManagers">Provider resolving the named secret manager that holds this identity's client secret.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="tokenEndpoint"/> or <paramref name="secretManagers"/> is null.</exception>
    public ClientCredentialsIdentityService(
        ILogger<ClientCredentialsIdentityService>? logger,
        ClientCredentialsConfiguration configuration,
        OAuth2TokenEndpointClient tokenEndpoint,
        Lazy<ISecretManagerProvider> secretManagers)
        : base(logger, configuration)
    {
        _tokenEndpoint = tokenEndpoint ?? throw new ArgumentNullException(nameof(tokenEndpoint));
        _secretManagers = secretManagers ?? throw new ArgumentNullException(nameof(secretManagers));
    }

    /// <inheritdoc/>
    public override async Task<IGenericResult<IssuedIdentityToken>> Acquire(
        IdentityTokenRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
            return GenericResult<IssuedIdentityToken>.Failure(IdentityLog.ConfigurationValueMissing(Logger, Name, nameof(request)));

        // NO FALLBACKS: every one of these is required to reach the authorization server at all. A
        // missing value is reported by name and fails, never substituted.
        if (Configuration.TokenEndpoint is not { Length: > 0 } tokenEndpoint)
            return GenericResult<IssuedIdentityToken>.Failure(IdentityLog.ConfigurationValueMissing(Logger, Name, nameof(Configuration.TokenEndpoint)));
        if (Configuration.Issuer is not { Length: > 0 } issuer)
            return GenericResult<IssuedIdentityToken>.Failure(IdentityLog.ConfigurationValueMissing(Logger, Name, nameof(Configuration.Issuer)));
        if (Configuration.ClientId is not { Length: > 0 } clientId)
            return GenericResult<IssuedIdentityToken>.Failure(IdentityLog.ConfigurationValueMissing(Logger, Name, nameof(Configuration.ClientId)));
        if (Configuration.SecretManagerName is not { Length: > 0 } secretManagerName)
            return GenericResult<IssuedIdentityToken>.Failure(IdentityLog.ConfigurationValueMissing(Logger, Name, nameof(Configuration.SecretManagerName)));
        if (Configuration.SecretKeyName is not { Length: > 0 } secretKeyName)
            return GenericResult<IssuedIdentityToken>.Failure(IdentityLog.ConfigurationValueMissing(Logger, Name, nameof(Configuration.SecretKeyName)));

        IdentityLog.AcquiringToken(Logger, Name, "ClientCredentials", request.Audience);

        var secretManager = await _secretManagers.Value.Get(secretManagerName, cancellationToken).ConfigureAwait(false);
        if (!secretManager.IsSuccess || secretManager.Value is null)
            return secretManager.ToNewResult<IssuedIdentityToken>();

        var secret = await secretManager.Value
            .Execute(GetSecretManagerCommand.Latest(container: null, secretKey: secretKeyName), cancellationToken)
            .ConfigureAwait(false);
        if (!secret.IsSuccess || secret.Value is null)
            return secret.ToNewResult<IssuedIdentityToken>();

        IdentityLog.ClientSecretResolved(Logger, Name, secretManagerName);

        using var clientSecretValue = secret.Value;
        if (clientSecretValue.GetStringValue() is not { Length: > 0 } clientSecret)
            return GenericResult<IssuedIdentityToken>.Failure(IdentityLog.ConfigurationValueMissing(Logger, Name, secretKeyName));

        return await _tokenEndpoint.Exchange(
            Name,
            tokenEndpoint,
            issuer,
            request,
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["client_id"] = clientId,
                ["client_secret"] = clientSecret,
            },
            cancellationToken).ConfigureAwait(false);
    }
}
