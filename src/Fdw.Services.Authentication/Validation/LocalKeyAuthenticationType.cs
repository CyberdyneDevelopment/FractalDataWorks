using System;
using System.Collections.Generic;
using Fdw.Collections;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions;
using Fdw.Services.Authentication.Logging;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Fdw.Services.Authentication.Validation;

/// <summary>
/// Validates bearer tokens this host issued itself, against the key it signed them with.
/// </summary>
/// <remarks>
/// <para>
/// The sibling of <see cref="JwtBearerAuthenticationType"/> for the one issuer a host does not need
/// to discover: itself. That option sets <c>Authority</c>, which sends ASP.NET to
/// <c>{authority}/.well-known/openid-configuration</c> and on to a JWKS endpoint — correct for a
/// remote provider, and for local tokens it would mean publishing a key this same process holds,
/// then fetching it back over the network to check a signature it just made.
/// </para>
/// <para>
/// The key comes from a secret manager, through the same signing credential provider the issuer
/// signs with. One place holds it, so the two sides cannot drift onto different keys, and rotating
/// it is a secret-manager operation rather than a deployment.
/// </para>
/// </remarks>
[ServiceTypeOption(typeof(AuthenticationServiceTypes), "LocalKey")]
public sealed class LocalKeyAuthenticationType : AuthenticationServiceTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="LocalKeyAuthenticationType"/> class.</summary>
    public LocalKeyAuthenticationType()
        : base("LocalKey",
               "Local Signing Key",
               "Validates bearer tokens this host issued, against the key it signed them with")
    {
        Registration((builder, loggerFactory) =>
        {
            builder.Services.TryAddSingleton<LocalKeyAuthenticationConfigurationProvider>(sp =>
                new LocalKeyAuthenticationConfigurationProvider(
                    sp.GetRequiredService<ILogger<LocalKeyAuthenticationConfigurationProvider>>(),
                    sp.GetRequiredService<IConfigurationGatewayProvider>(),
                    AuthenticationServiceTypes.ConfigurationConnection,
                    AuthenticationServiceTypes.ServerConfigurationPath));
            builder.Services.TryAddSingleton<ILocalKeyAuthenticationConfigurationProvider>(sp =>
                sp.GetRequiredService<LocalKeyAuthenticationConfigurationProvider>());
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });

        // Initialize, because both providers have to be resolvable: the option is the only thing that
        // knows which implementation it is, and the domain provider dispatches by the name registered
        // here. Without this the domain row names a kind the registry has never heard of, and the
        // read fails at the point a token arrives rather than at startup.
        Initialization((host, loggerFactory) =>
        {
            host.Services.GetRequiredService<IAuthenticationServiceConfigurationProvider>()
                .Register(Name, host.Services.GetRequiredService<LocalKeyAuthenticationConfigurationProvider>());
            return GenericResult<IHost>.Success(host);
        });
    }

    /// <inheritdoc />
    public override string[] SupportedProtocols => ["OAuth2"];

    /// <inheritdoc />
    public override string ProviderName => "Microsoft.AspNetCore.Authentication.JwtBearer";

    /// <inheritdoc />
    public override IReadOnlyList<string> SupportedFlows => ["Password", "ClientCredentials"];

    /// <inheritdoc />
    public override IReadOnlyList<string> SupportedTokenTypes => ["AccessToken"];

    /// <inheritdoc />
    /// <remarks>
    /// Ahead of the remote options: a host's own issuer is the one it can check without a network
    /// call, so trying it first costs nothing when it does not match.
    /// </remarks>
    public override int Priority => 10;

    /// <inheritdoc />
    public override bool SupportsMultiTenant => true;

    /// <inheritdoc />
    public override bool SupportsTokenCaching => false;

    /// <summary>The prefix every LocalKey scheme name carries.</summary>
    /// <remarks>
    /// Distinct from the JwtBearer prefix so two entries of different kinds cannot land on one
    /// scheme name — ASP.NET would take the second as a duplicate of the first. It is also what the
    /// options bridge reads the entry name back out of.
    /// </remarks>
    public const string SchemePrefix = "Fdw.LocalKey.";

    /// <summary>The scheme name this option registers for a given entry.</summary>
    /// <param name="serviceName">The declared entry's name.</param>
    public static string SchemeNameFor(string serviceName) => SchemePrefix + serviceName;

    /// <inheritdoc />
    public override IGenericResult<AuthenticationSchemeBinding> TakeScheme(
        IAuthenticationServiceConfiguration configuration,
        IAuthenticationSchemeProvider schemes,
        IServiceProvider services,
        ILoggerFactory? loggerFactory)
    {
        if (configuration is null) throw new ArgumentNullException(nameof(configuration));
        if (schemes is null) throw new ArgumentNullException(nameof(schemes));

        var log = loggerFactory?.CreateLogger<LocalKeyAuthenticationType>()
            ?? NullLogger<LocalKeyAuthenticationType>.Instance;

        if (configuration.Name is not { Length: > 0 } serviceName)
            return GenericResult<AuthenticationSchemeBinding>.Failure(
                AuthenticationValidationLog.EntryMissingName(log, "(unnamed)"));

        if (configuration.Authority is not { Length: > 0 } issuer)
            return GenericResult<AuthenticationSchemeBinding>.Failure(
                AuthenticationValidationLog.EntryMissingAuthority(log, serviceName));

        // The scheme is added here; its TokenValidationParameters are read from this entry's
        // implementation row on first use, by the options bridge. Adding a scheme twice throws, and a
        // host that declares one issuer twice is a configuration defect worth reporting.
        schemes.AddScheme(new AuthenticationScheme(
            SchemeNameFor(serviceName), displayName: null, handlerType: typeof(JwtBearerHandler)));

        return GenericResult<AuthenticationSchemeBinding>.Success(
            new AuthenticationSchemeBinding(serviceName, issuer, SchemeNameFor(serviceName)));
    }
}
