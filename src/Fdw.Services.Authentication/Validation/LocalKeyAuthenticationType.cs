using System;
using System.Collections.Generic;
using Fdw.Collections;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions;
using Fdw.Services.Authentication.Logging;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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

    /// <summary>The scheme name this option registers for a given entry.</summary>
    /// <param name="serviceName">The declared entry's name.</param>
    /// <remarks>
    /// Distinct from the JwtBearer prefix so two entries of different kinds cannot land on one
    /// scheme name — ASP.NET would take the second registration as a duplicate of the first.
    /// </remarks>
    public static string SchemeNameFor(string serviceName) => "Fdw.LocalKey." + serviceName;

    /// <inheritdoc />
    public override IGenericResult<AuthenticationSchemeBinding> RegisterScheme(
        AuthenticationBuilder authenticationBuilder,
        AuthenticationServiceConfiguration configuration,
        IConfigurationSection section,
        IServiceCollection services,
        ILoggerFactory? loggerFactory)
    {
        if (authenticationBuilder is null) throw new ArgumentNullException(nameof(authenticationBuilder));
        if (configuration is null) throw new ArgumentNullException(nameof(configuration));
        if (services is null) throw new ArgumentNullException(nameof(services));

        var log = loggerFactory?.CreateLogger<LocalKeyAuthenticationType>()
            ?? NullLogger<LocalKeyAuthenticationType>.Instance;

        if (configuration.Name is not { Length: > 0 } serviceName)
            return GenericResult<AuthenticationSchemeBinding>.Failure(
                AuthenticationValidationLog.EntryMissingName(log, section.Path));

        if (configuration.Authority is not { Length: > 0 } issuer)
            return GenericResult<AuthenticationSchemeBinding>.Failure(
                AuthenticationValidationLog.EntryMissingAuthority(log, serviceName));

        var typed = LocalKeyAuthenticationConfiguration.Read(section, serviceName, log);
        if (typed.IsFailure)
            return typed.ToNewResult<AuthenticationSchemeBinding>();

        if (typed.Value is not { } body)
            return GenericResult<AuthenticationSchemeBinding>.Failure(
                AuthenticationValidationLog.LocalKeyMissingAudience(log, serviceName));

        var schemeName = SchemeNameFor(serviceName);

        // The scheme is declared here and its key supplied by ConfigureLocalKeyScheme, which runs
        // inside the built container: reading a secret needs the secret manager, and at this point
        // nothing has been registered that could resolve one.
        services.AddSingleton<IConfigureNamedOptions<JwtBearerOptions>>(serviceProvider =>
            new ConfigureLocalKeyScheme(schemeName, issuer, body.Audience, serviceProvider));

        authenticationBuilder.AddJwtBearer(schemeName, _ => { });

        return GenericResult<AuthenticationSchemeBinding>.Success(
            new AuthenticationSchemeBinding(serviceName, issuer, schemeName));
    }
}
