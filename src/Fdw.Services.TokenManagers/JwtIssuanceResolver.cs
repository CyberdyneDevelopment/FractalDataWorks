using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Fdw.Results;
using Fdw.Services.SecretManagers.Abstractions;
using Fdw.Services.TokenManagers.Abstractions;
using Fdw.Services.TokenManagers.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.TokenManagers;

/// <summary>
/// Builds the issuer and its signing key from configuration, once, on first use.
/// </summary>
/// <remarks>
/// <para>
/// What this host signs as lives in <c>auth.TokenManager</c> and its <c>auth.JwtTokenManager</c>
/// body — rows in a database reached through a gateway that does not exist during the registration
/// phase. So registration declares the two seams and this resolves them the first time something
/// actually mints or validates, which is the first moment the rows can be read.
/// </para>
/// <para>
/// The minting and validating halves come from here together rather than being configured
/// separately. They have to agree on the key and the issuer, and two independent reads of the same
/// rows is how they drift.
/// </para>
/// </remarks>
internal sealed class JwtIssuanceResolver : IDisposable
{
    private readonly IServiceProvider _services;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly ILogger<JwtIssuanceResolver> _logger;

    private JwtTokenIssuer? _issuer;
    private ISigningCredentialProvider? _credentials;

    /// <summary>Initializes a new instance of the <see cref="JwtIssuanceResolver"/> class.</summary>
    /// <param name="services">The container the providers come from.</param>
    /// <param name="logger">The logger.</param>
    public JwtIssuanceResolver(IServiceProvider services, ILogger<JwtIssuanceResolver>? logger = null)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _logger = logger ?? NullLogger<JwtIssuanceResolver>.Instance;
    }

    /// <summary>Gets the issuer this host mints with.</summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    public async Task<IGenericResult<ITokenIssuer>> Issuer(CancellationToken cancellationToken = default)
    {
        var ensured = await Ensure(cancellationToken).ConfigureAwait(false);

        return ensured.IsFailure
            ? ensured.ToNewResult<ITokenIssuer>()
            : GenericResult<ITokenIssuer>.Success(_issuer!);
    }

    /// <summary>Gets the key this host signs and validates with.</summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    public async Task<IGenericResult<ISigningCredentialProvider>> Credentials(
        CancellationToken cancellationToken = default)
    {
        var ensured = await Ensure(cancellationToken).ConfigureAwait(false);

        return ensured.IsFailure
            ? ensured.ToNewResult<ISigningCredentialProvider>()
            : GenericResult<ISigningCredentialProvider>.Success(_credentials!);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _gate.Dispose();
        (_credentials as IDisposable)?.Dispose();
    }

    private async Task<IGenericResult> Ensure(CancellationToken cancellationToken)
    {
        if (_issuer is not null && _credentials is not null)
            return GenericResult.Success();

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            if (_issuer is not null && _credentials is not null)
                return GenericResult.Success();

            var headers = await _services
                .GetRequiredService<TokenManagerConfigurationProvider>()
                .Get(cancellationToken)
                .ConfigureAwait(false);

            if (headers.IsFailure || headers.Value is not { } rows)
                return GenericResult.Failure(
                    IssuerLog.HeadersUnreadable(_logger, headers.CurrentMessage));

            if (Select(rows) is not { } header)
                return GenericResult.Failure(IssuerLog.NoJwtTokenManager(
                    _logger,
                    rows.Count == 0
                        ? "no rows"
                        : string.Join(", ", rows.Select(r => r.ServiceOptionType ?? "(none)"))));

            if (header.SecretManagerName is not { Length: > 0 } secretManager
                || header.SecretKeyName is not { Length: > 0 } secretKey)
                return GenericResult.Failure(IssuerLog.SigningKeyNotLocatable(_logger, header.Name));

            var typed = await _services
                .GetRequiredService<JwtTokenManagerConfigurationProvider>()
                .Get(header.Id, cancellationToken)
                .ConfigureAwait(false);

            if (typed.IsFailure || typed.Value is not JwtTokenManagerConfiguration body)
                return GenericResult.Failure(
                    IssuerLog.TypedBodyUnreadable(_logger, header.Name, typed.CurrentMessage));

            if (body.Issuer is not { Length: > 0 } issuer)
                return GenericResult.Failure(IssuerLog.IssuerMissing(_logger, header.Name));

            var lifetime = Lifetime(body.AccessTokenLifetime, header.Name);
            if (lifetime.IsFailure)
                return lifetime;

            var credentials = new SecretManagerSigningCredentialProvider(
                _services.GetRequiredService<ISecretManagerProvider>(),
                secretManager,
                secretKey,
                TimeSpan.FromMinutes(10),
                _services.GetService<ILogger<SecretManagerSigningCredentialProvider>>());

            _issuer = new JwtTokenIssuer(
                new JwtTokenIssuerConfiguration { Issuer = issuer, Lifetime = lifetime.Value },
                credentials,
                _services.GetService<ILogger<JwtTokenIssuer>>());

            _credentials = credentials;

            IssuerLog.IssuanceResolved(_logger, header.Name, issuer);
            return GenericResult.Success();
        }
        finally
        {
            _gate.Release();
        }
    }

    // Matched on the option type rather than a configured name: the name is a label someone chose,
    // and which option a row declares is what decides whether this code can serve it.
    private static TokenManagerConfiguration? Select(IReadOnlyList<TokenManagerConfiguration> rows) =>
        rows.FirstOrDefault(row =>
            string.Equals(row.ServiceOptionType, "Jwt", StringComparison.OrdinalIgnoreCase));

    private IGenericResult<TimeSpan> Lifetime(string? configured, string name)
    {
        if (configured is not { Length: > 0 } value)
            return GenericResult<TimeSpan>.Failure(IssuerLog.LifetimeMissing(_logger, name));

        try
        {
            return GenericResult<TimeSpan>.Success(XmlConvert.ToTimeSpan(value));
        }
        catch (FormatException ex)
        {
            return GenericResult<TimeSpan>.Failure(
                IssuerLog.LifetimeUnreadable(_logger, ex, name, value));
        }
    }
}
