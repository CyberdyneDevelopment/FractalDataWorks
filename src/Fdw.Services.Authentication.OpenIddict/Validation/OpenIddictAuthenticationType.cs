using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Results;
using Fdw.Services.Authentication.Logging;
using Fdw.Services.Authentication.Validation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using OpenIddict.Server;
using OpenIddict.Validation.AspNetCore;

namespace Fdw.Services.Authentication.OpenIddict.Validation;

/// <summary>
/// Validates tokens issued by FDW's own OpenIddict authority — the ones a signed-in person holds, and
/// the ones a proxy forwards on their behalf.
/// </summary>
/// <remarks>
/// <para>
/// The scheme itself is registered by the OpenIddict token-manager option, which owns the whole
/// OpenIddict pipeline: a host that issues tokens validates them with the co-resident server's own
/// keys. What this option adds is the one fact that pipeline does not publish — which issuer those
/// tokens come from — so <see cref="IssuerSchemeSelector"/> can route to it beside issuers this host
/// does not run.
/// </para>
/// <para>
/// That makes the declared authority load-bearing in a way it was not before, which is why
/// <c>Initialize</c> checks it against the issuer OpenIddict actually stamps. The two are written in
/// different places — this host's <c>AuthenticationServices</c> entry and the <c>auth.TokenManager</c>
/// row the authority reads — and if they drift, every token this host issues routes to the scheme that
/// accepts nothing. A boot failure naming both values is a better place to find that out than a 401 on
/// every request.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[ServiceTypeOption(typeof(AuthenticationServiceTypes), "OpenIddict")]
public sealed class OpenIddictAuthenticationType : AuthenticationServiceTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="OpenIddictAuthenticationType"/> class.</summary>
    public OpenIddictAuthenticationType()
        : base("OpenIddict",
               "OpenIddict",
               "Validates bearer tokens issued by this deployment's own OpenIddict authority")
    {
        Initialization((host, loggerFactory) =>
        {
            var log = loggerFactory?.CreateLogger<OpenIddictAuthenticationType>()
                ?? NullLogger<OpenIddictAuthenticationType>.Instance;

            var declared = AuthenticationServiceConfiguration.Read(
                host.Services.GetRequiredService<IConfiguration>(), Name, log);
            if (declared.IsFailure)
                return declared.ToNewResult<IHost>();
            if (declared.Value is not { } entries)
                return GenericResult<IHost>.Failure(AuthenticationValidationLog.SectionUnreadable(log, Name));

            var stamped = host.Services.GetRequiredService<IOptions<OpenIddictServerOptions>>().Value.Issuer;
            if (stamped is null)
                return GenericResult<IHost>.Failure(
                    AuthenticationValidationLog.OpenIddictIssuerNotStamped(log));

            foreach (var (header, _) in entries)
            {
                if (!string.Equals(header.Authority, stamped.AbsoluteUri, StringComparison.Ordinal))
                    return GenericResult<IHost>.Failure(
                        AuthenticationValidationLog.OpenIddictIssuerMismatch(
                            log, header.Name ?? Name, header.Authority ?? string.Empty, stamped.AbsoluteUri));
            }

            return GenericResult<IHost>.Success(host);
        });
    }

    /// <inheritdoc />
    public override string[] SupportedProtocols => ["OAuth2", "OpenIDConnect"];

    /// <inheritdoc />
    public override string ProviderName => "OpenIddict";

    /// <inheritdoc />
    public override IReadOnlyList<string> SupportedFlows =>
        ["AuthorizationCode", "ClientCredentials", "Interactive", "Silent"];

    /// <inheritdoc />
    public override IReadOnlyList<string> SupportedTokenTypes => ["AccessToken", "IdToken", "RefreshToken"];

    /// <inheritdoc />
    public override int Priority => 100;

    /// <inheritdoc />
    public override bool SupportsMultiTenant => true;

    /// <inheritdoc />
    public override bool SupportsTokenCaching => false;

    /// <inheritdoc />
    /// <remarks>
    /// Adds no scheme. The OpenIddict token-manager option registered the validation handler as part of
    /// the pipeline it owns, and a second registration of the same scheme name is an error rather than a
    /// duplicate. This reports which scheme that was, and the issuer it accepts.
    /// </remarks>
    public override IGenericResult<AuthenticationSchemeBinding> RegisterScheme(
        AuthenticationBuilder authenticationBuilder,
        AuthenticationServiceConfiguration configuration,
        IConfigurationSection section,
        IServiceCollection services,
        ILoggerFactory? loggerFactory)
    {
        if (configuration is null) throw new ArgumentNullException(nameof(configuration));
        if (section is null) throw new ArgumentNullException(nameof(section));

        var log = loggerFactory?.CreateLogger<OpenIddictAuthenticationType>()
            ?? NullLogger<OpenIddictAuthenticationType>.Instance;

        if (configuration.Name is not { Length: > 0 } serviceName)
            return GenericResult<AuthenticationSchemeBinding>.Failure(
                AuthenticationValidationLog.EntryMissingName(log, section.Path));
        if (configuration.Authority is not { Length: > 0 } authority)
            return GenericResult<AuthenticationSchemeBinding>.Failure(
                AuthenticationValidationLog.EntryMissingAuthority(log, serviceName));

        return GenericResult<AuthenticationSchemeBinding>.Success(
            new AuthenticationSchemeBinding(
                serviceName, authority, OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme));
    }
}
