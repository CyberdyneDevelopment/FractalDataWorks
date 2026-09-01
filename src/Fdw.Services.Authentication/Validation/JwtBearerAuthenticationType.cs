using System;
using System.Threading;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Fdw.Collections;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions;
using Fdw.Services.Authentication.Logging;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Authentication.Validation;

/// <summary>
/// Validates tokens from a remote OpenID Connect issuer — one this host does not run, and whose signing
/// keys it reads from the issuer's published JWKS.
/// </summary>
/// <remarks>
/// This is the mechanism a service-to-service call from outside FDW's own auth server arrives on: a
/// scheduled dispatch holding a corporate IdP's client-credentials token, a partner system, a CI job.
/// The token proves who called; the entry's declared roles say what that caller may do here.
/// </remarks>
[ExcludeFromCodeCoverage]
[ServiceTypeOption(typeof(AuthenticationServiceTypes), "JwtBearer")]
public sealed class JwtBearerAuthenticationType : AuthenticationServiceTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="JwtBearerAuthenticationType"/> class.</summary>
    public JwtBearerAuthenticationType()
        : base("JwtBearer",
               "JWT Bearer",
               "Validates bearer tokens issued by a remote OpenID Connect provider against its published JWKS")
    {
        Registration((builder, loggerFactory) =>
        {
            builder.Services.TryAddSingleton<JwtBearerAuthenticationConfigurationProvider>(sp =>
                new JwtBearerAuthenticationConfigurationProvider(
                    sp.GetRequiredService<ILogger<JwtBearerAuthenticationConfigurationProvider>>(),
                    sp.GetRequiredService<IConfigurationGatewayProvider>(),
                    AuthenticationServiceTypes.ConfigurationConnection,
                    AuthenticationServiceTypes.ServerConfigurationPath));
            builder.Services.TryAddSingleton<IJwtBearerAuthenticationConfigurationProvider>(sp =>
                sp.GetRequiredService<JwtBearerAuthenticationConfigurationProvider>());

            // Transient for the same reason LocalKey's is: the handler holds the scheme and the
            // request it was initialised for in fields, so one instance per resolution is required.
            builder.Services.TryAddTransient<JwtBearerAuthenticationHandler>();
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });

        // Initialize, because both providers have to be resolvable: the option is the only thing that
        // knows which implementation it is, and the domain provider dispatches by the name registered
        // here. Without this the domain row names a kind the registry has never heard of, and the
        // read fails at the point a token arrives rather than at startup.
        Initialization((host, loggerFactory) =>
        {
            host.Services.GetRequiredService<IAuthenticationServiceConfigurationProvider>()
                .Register(Name, host.Services.GetRequiredService<JwtBearerAuthenticationConfigurationProvider>());
            return GenericResult<IHost>.Success(host);
        });
    }

    /// <inheritdoc />
    public override string[] SupportedProtocols => ["OAuth2", "OpenIDConnect"];

    /// <inheritdoc />
    public override string ProviderName => "Microsoft.AspNetCore.Authentication.JwtBearer";

    /// <inheritdoc />
    public override IReadOnlyList<string> SupportedFlows => ["ClientCredentials", "AuthorizationCode"];

    /// <inheritdoc />
    public override IReadOnlyList<string> SupportedTokenTypes => ["AccessToken"];

    /// <inheritdoc />
    public override int Priority => 50;

    /// <inheritdoc />
    public override bool SupportsMultiTenant => false;

    /// <inheritdoc />
    public override bool SupportsTokenCaching => false;

    /// <inheritdoc />
    public override IGenericResult<AuthenticationSchemeBinding> TakeScheme(
        IAuthenticationServiceConfiguration configuration,
        IAuthenticationSchemeProvider schemes,
        IServiceProvider services,
        ILoggerFactory? loggerFactory)
    {
        if (configuration is null) throw new ArgumentNullException(nameof(configuration));
        if (schemes is null) throw new ArgumentNullException(nameof(schemes));

        var log = loggerFactory?.CreateLogger<JwtBearerAuthenticationType>()
            ?? NullLogger<JwtBearerAuthenticationType>.Instance;

        if (configuration.Name is not { Length: > 0 } serviceName)
            return GenericResult<AuthenticationSchemeBinding>.Failure(
                AuthenticationValidationLog.EntryMissingName(log, "(unnamed)"));

        if (configuration.Authority is not { Length: > 0 } authority)
            return GenericResult<AuthenticationSchemeBinding>.Failure(
                AuthenticationValidationLog.EntryMissingAuthority(log, serviceName));

        schemes.AddScheme(new AuthenticationScheme(
            SchemeNameFor(serviceName), displayName: null, handlerType: typeof(JwtBearerAuthenticationHandler)));

        return GenericResult<AuthenticationSchemeBinding>.Success(
            new AuthenticationSchemeBinding(serviceName, authority, SchemeNameFor(serviceName)));
    }

    /// <summary>
    /// Builds the ASP.NET scheme name for an authentication service.
    /// </summary>
    /// <returns>The scheme name.</returns>
    /// <remarks>
    /// Qualified by mechanism so two services of different mechanisms can share a name without one
    /// silently replacing the other's scheme options.
    /// </remarks>
    public const string SchemePrefix = "Fdw.JwtBearer.";

    /// <summary>The scheme name this option registers for a given entry.</summary>
    /// <param name="serviceName">The declared entry's name.</param>
    public static string SchemeNameFor(string serviceName) => SchemePrefix + serviceName;
}
