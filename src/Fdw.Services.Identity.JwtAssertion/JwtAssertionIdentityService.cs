using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Identity.Abstractions;
using Fdw.Services.Identity.Logging;
using Fdw.Services.Identity.JwtAssertion.Assertions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Identity.JwtAssertion;

/// <summary>
/// Proves this workload's identity by presenting an assertion an external OIDC issuer
/// already minted for it (RFC 7523 client assertion), which the authorization server validates against that
/// issuer's published keys.
/// </summary>
/// <remarks>
/// No static secret is involved at any point. The assertion is read fresh on every acquisition
/// because the platform that mints it rotates it in place — holding one would pin the first
/// assertion and start failing silently once it expired.
/// </remarks>
public sealed class JwtAssertionIdentityService
    : IdentityServiceBase<JwtAssertionConfiguration, JwtAssertionIdentityService>
{
    private const string JwtBearerAssertionType = "urn:ietf:params:oauth:client-assertion-type:jwt-bearer";

    private readonly OAuth2TokenEndpointClient _tokenEndpoint;

    /// <summary>
    /// Initializes a new instance of the <see cref="JwtAssertionIdentityService"/> class.
    /// </summary>
    /// <param name="logger">The logger for this service.</param>
    /// <param name="configuration">The typed configuration body for this identity.</param>
    /// <param name="tokenEndpoint">The shared token-endpoint client.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="tokenEndpoint"/> is null.</exception>
    public JwtAssertionIdentityService(
        ILogger<JwtAssertionIdentityService>? logger,
        JwtAssertionConfiguration configuration,
        OAuth2TokenEndpointClient tokenEndpoint)
        : base(logger, configuration)
    {
        _tokenEndpoint = tokenEndpoint ?? throw new ArgumentNullException(nameof(tokenEndpoint));
    }

    /// <inheritdoc/>
    public override async Task<IGenericResult<IssuedIdentityToken>> Acquire(
        IdentityTokenRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
            return GenericResult<IssuedIdentityToken>.Failure(IdentityLog.ConfigurationValueMissing(Logger, Name, nameof(request)));

        // NO FALLBACKS: every one of these is required to reach the provider at all. A missing value
        // is reported by name and fails, never substituted.
        if (Configuration.TokenEndpoint is not { Length: > 0 } tokenEndpoint)
            return GenericResult<IssuedIdentityToken>.Failure(IdentityLog.ConfigurationValueMissing(Logger, Name, nameof(Configuration.TokenEndpoint)));
        if (Configuration.Issuer is not { Length: > 0 } issuer)
            return GenericResult<IssuedIdentityToken>.Failure(IdentityLog.ConfigurationValueMissing(Logger, Name, nameof(Configuration.Issuer)));
        if (Configuration.ClientId is not { Length: > 0 } clientId)
            return GenericResult<IssuedIdentityToken>.Failure(IdentityLog.ConfigurationValueMissing(Logger, Name, nameof(Configuration.ClientId)));
        if (Configuration.AssertionSource is not { Length: > 0 } assertionSource)
            return GenericResult<IssuedIdentityToken>.Failure(IdentityLog.ConfigurationValueMissing(Logger, Name, nameof(Configuration.AssertionSource)));
        if (Configuration.AssertionLocation is not { Length: > 0 } assertionLocation)
            return GenericResult<IssuedIdentityToken>.Failure(IdentityLog.ConfigurationValueMissing(Logger, Name, nameof(Configuration.AssertionLocation)));

        IdentityLog.AcquiringToken(Logger, Name, "JwtAssertion", request.Audience);

        var source = FederatedAssertionSources.ByName(assertionSource);
        if (source == FederatedAssertionSources.NotFound)
            return GenericResult<IssuedIdentityToken>.Failure(IdentityLog.ConfigurationValueMissing(Logger, Name, nameof(Configuration.AssertionSource)));

        var assertion = source.Read(Name, assertionLocation, Logger);
        if (assertion.IsFailure || assertion.Value is not { Length: > 0 } assertionValue)
            return assertion.ToNewResult<IssuedIdentityToken>();

        IdentityLog.AssertionLocated(Logger, Name, assertionSource, assertionLocation);

        return await _tokenEndpoint.Exchange(
            Name,
            tokenEndpoint,
            issuer,
            request,
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["client_id"] = clientId,
                ["client_assertion_type"] = JwtBearerAssertionType,
                ["client_assertion"] = assertionValue,
            },
            cancellationToken).ConfigureAwait(false);
    }
}
