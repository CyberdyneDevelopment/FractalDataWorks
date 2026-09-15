using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Configuration;
using Fdw.Services.Abstractions;
using Fdw.Services.TokenManagers.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;

namespace Fdw.Services.TokenManagers;

/// <summary>
/// Registers the token-issuance and -revocation services every deployment needs — a single, hand-written
/// three-phase registrant, not a <c>[ServiceTypeCollection]</c> dispatch: FDW-672 replaced the token-manager
/// selection this domain used to dispatch to with the step pipeline, and no <c>[TypeOption]</c> was ever
/// declared against the old collection. What remains genuinely live — issuance via
/// <see cref="JwtIssuanceResolver"/> and revocation via <see cref="ITokenRevocationStore"/> — is registered
/// directly, matching how <c>JwtIssuanceResolver</c> itself already bypasses per-option dispatch.
/// </summary>
[ExcludeFromCodeCoverage]
[PlatformServiceProvider(ServiceCategory = "TokenManager")]
public static class TokenManagerTypes
{
    /// <summary>
    /// The connection this domain's configuration rows are read from.
    /// </summary>
    public static string ConfigurationConnection { get; set; } = "PlatformConfiguration";

    /// <summary>
    /// The implementation name the Jwt token manager is registered under -- the value a token-manager
    /// domain row carries in <c>Implementation</c>.
    /// </summary>
    private const string JwtImplementation = "Jwt";

    /// <summary>
    /// No-op — this domain has no pre-Build IOptions binding to perform; <see cref="Register"/> does the
    /// collection's only real work. Declared only so the <c>[PlatformServiceProvider]</c> three-phase
    /// shape requirement is satisfied.
    /// </summary>
    /// <param name="builder">The host application builder.</param>
    /// <param name="loggerFactory">Unused.</param>
    /// <param name="force">Run regardless of the skip flag and whether the phase has already run.</param>
    /// <param name="defer">Claim the phase without running it: the collect skips it and the next explicit call runs it.</param>
    public static IGenericResult<IHostApplicationBuilder> Configure(IHostApplicationBuilder builder, ILoggerFactory? loggerFactory = null, bool force = false, bool defer = false)
        => GenericResult<IHostApplicationBuilder>.Success(builder);

    /// <summary>
    /// Registers issuance (<see cref="ITokenIssuer"/>/<see cref="ISigningCredentialProvider"/> via
    /// <see cref="JwtIssuanceResolver"/>) and revocation (<see cref="ITokenRevocationStore"/>) — the two
    /// services this domain provides. One registration for the whole domain, not per-option: a
    /// deployment mints as exactly one issuer, and a host that had to call an AddXxx of its own is a
    /// host the next one has to remember to copy.
    /// </summary>
    /// <param name="builder">The host application builder.</param>
    /// <param name="loggerFactory">Optional logger factory for registration logging.</param>
    /// <param name="force">Run regardless of the skip flag and whether the phase has already run.</param>
    /// <param name="defer">Claim the phase without running it: the collect skips it and the next explicit call runs it.</param>
    public static IGenericResult<IHostApplicationBuilder> Register(IHostApplicationBuilder builder, ILoggerFactory? loggerFactory = null, bool force = false, bool defer = false)
    {
        // Why: defer is the host claiming this phase to run at a position it chooses.
        // Stateless, so there is no flag to set - returning is the whole of it.
        if (defer)
            return GenericResult<IHostApplicationBuilder>.Success(builder);

            builder.Services.AddSingleton<ITokenManagerConfigurationProvider, TokenManagerConfigurationProvider>(sp => new TokenManagerConfigurationProvider(sp.GetRequiredService<ILogger<TokenManagerConfigurationProvider>>(), sp.GetRequiredService<IConfigurationGatewayProvider>(), TokenManagerTypes.ConfigurationConnection));

        builder.Services.AddSingleton<IJwtTokenManagerConfigurationProvider, JwtTokenManagerConfigurationProvider>(sp => new JwtTokenManagerConfigurationProvider(sp.GetRequiredService<ILogger<JwtTokenManagerConfigurationProvider>>(), sp.GetRequiredService<IConfigurationGatewayProvider>(), TokenManagerTypes.ConfigurationConnection));

        builder.Services.TryAddSingleton<IAuthenticationContextAccessor, AuthenticationContextAccessor>();
        builder.Services.TryAddSingleton<JwtIssuanceResolver>(sp =>
            new JwtIssuanceResolver(sp, sp.GetRequiredService<IAuthenticationContextAccessor>(), sp.GetService<ILogger<JwtIssuanceResolver>>()));

        builder.Services.TryAddSingleton<ITokenIssuer>(sp =>
            new ConfiguredTokenIssuer(sp.GetRequiredService<JwtIssuanceResolver>()));

        builder.Services.TryAddSingleton<ISigningCredentialProvider>(sp =>
            new ConfiguredSigningCredentialProvider(sp.GetRequiredService<JwtIssuanceResolver>()));

        // Scoped, not singleton: the store holds no state of its own, reads and writes AuthDb on every
        // call through IDataGatewayProvider, and that provider is scoped -- matching IDataGateway's own
        // lifetime. Its only consumer, LocalKeyAuthenticationHandler, is itself per-request under
        // ASP.NET's authentication-handler convention, so scoped costs nothing here.
        builder.Services.TryAddScoped<ITokenRevocationStore, TokenRevocationStore>();

        return GenericResult<IHostApplicationBuilder>.Success(builder);
    }

    /// <summary>
    /// Hands the Jwt implementation configuration provider to the token-manager domain, so a domain row
    /// naming <c>Implementation = "Jwt"</c> can be composed.
    /// </summary>
    /// <remarks>
    /// This was a no-op, and <see cref="Register"/> put both providers in the container without ever
    /// connecting them -- so <see cref="JwtIssuanceResolver"/> read the <c>ApiTokenIssuer</c> row, found no
    /// implementation registered under "Jwt", and every login failed at issuance with "cannot compose
    /// 'ApiTokenIssuer'". It has to happen here rather than in <see cref="Register"/>: the registry lives
    /// on the provider instance, and there is no instance until the container is built.
    /// </remarks>
    /// <param name="host">The built host.</param>
    /// <param name="loggerFactory">Unused.</param>
    /// <param name="force">Run regardless of the skip flag and whether the phase has already run.</param>
    /// <param name="defer">Claim the phase without running it: the collect skips it and the next explicit call runs it.</param>
    public static IGenericResult<IHost> Initialize(IHost host, ILoggerFactory? loggerFactory = null, bool force = false, bool defer = false)
    {
        if (defer)
            return GenericResult<IHost>.Success(host);

        var registered = host.Services.GetRequiredService<ITokenManagerConfigurationProvider>()
            .Register(JwtImplementation, host.Services.GetRequiredService<IJwtTokenManagerConfigurationProvider>());
        return registered.IsSuccess
            ? GenericResult<IHost>.Success(host)
            : registered.ToNewResult<IHost>();
    }
}
