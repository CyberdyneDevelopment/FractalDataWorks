using System;
using System.Threading;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Fdw.Collections;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions;
using Fdw.Services.Authentication.Logging;
using Fdw.Services.Authorization.Abstractions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
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

        // UNFINISHED. This still names JwtBearerHandler, which reads JwtBearerOptions from
        // IOptionsMonitor - and nothing populates them, so a declared JwtBearer entry takes a scheme
        // that validates with no authority, no audience and no key, and refuses every token from that
        // issuer. Latent only because no host declares one today. LocalKey has been moved off the
        // options system onto its own IAuthenticationHandler; this option needs the same treatment,
        // plus the implementation configuration, provider and container declaration it also lacks.
        schemes.AddScheme(new AuthenticationScheme(
            SchemeNameFor(serviceName), displayName: null, handlerType: typeof(JwtBearerHandler)));

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
    public static string SchemeNameFor(string serviceName) => "Fdw.JwtBearer." + serviceName;

    private static async Task ConferDeclaredRoles(
        TokenValidatedContext context,
        string serviceName,
        IReadOnlyList<string> roles)
    {
        var services = context.HttpContext.RequestServices;
        var log = services.GetService<ILoggerFactory>()?.CreateLogger<JwtBearerAuthenticationType>()
            ?? NullLogger<JwtBearerAuthenticationType>.Instance;

        if (context.Principal?.Identity is not ClaimsIdentity identity)
        {
            AuthenticationValidationLog.ValidatedTokenHasNoIdentity(log, serviceName);
            context.Fail("The validated token produced no claims identity.");
            return;
        }

        var permissions = await services.GetRequiredService<IRolePermissionResolver>()
            .Resolve(roles, context.HttpContext.RequestAborted).ConfigureAwait(false);

        if (permissions.IsFailure)
        {
            AuthenticationValidationLog.DeclaredRolesNotResolved(
                log, serviceName, string.Join(", ", roles), permissions.CurrentMessage ?? string.Empty);
            context.Fail(permissions.CurrentMessage ?? "The declared roles could not be resolved to permissions.");
            return;
        }

        if (permissions.Value is not { } granted)
        {
            AuthenticationValidationLog.DeclaredRolesNotResolved(
                log, serviceName, string.Join(", ", roles), "the resolver reported success with no permission set");
            context.Fail("The declared roles resolved to no permission set.");
            return;
        }

        foreach (var role in roles)
            identity.AddClaim(new Claim(ClaimDefinitions.roles.Name, role));

        foreach (var permission in granted)
            identity.AddClaim(new Claim(ClaimDefinitions.perm.Name, permission));

        AuthenticationValidationLog.DeclaredRolesConferred(
            log, serviceName, roles.Count, granted.Count,
            identity.FindFirst(ClaimDefinitions.sub.Name)?.Value ?? "(no sub)");
    }
}
