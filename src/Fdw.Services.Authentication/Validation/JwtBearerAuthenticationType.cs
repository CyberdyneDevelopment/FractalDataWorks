using System;
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
using Microsoft.Extensions.Configuration;
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
    // Why false: the roles are declared per issuer, not per tenant, so one of these schemes speaks for
    // one identity. A tenant-aware external issuer would be a different mechanism, declaring how it
    // carries the tenant.
    public override bool SupportsMultiTenant => false;

    /// <inheritdoc />
    // Why false: this validates a token that is presented to it. Nothing here acquires or holds one —
    // that is the outbound side, and it is IIdentityService's.
    public override bool SupportsTokenCaching => false;

    /// <inheritdoc />
    public override IGenericResult<AuthenticationSchemeBinding> RegisterScheme(
        AuthenticationBuilder authenticationBuilder,
        AuthenticationServiceConfiguration configuration,
        IConfigurationSection section,
        ILoggerFactory? loggerFactory)
    {
        if (authenticationBuilder is null) throw new ArgumentNullException(nameof(authenticationBuilder));
        if (configuration is null) throw new ArgumentNullException(nameof(configuration));

        var log = loggerFactory?.CreateLogger<JwtBearerAuthenticationType>()
            ?? NullLogger<JwtBearerAuthenticationType>.Instance;

        // The base validated these before calling; reading them again is what makes that visible to the
        // compiler rather than asserted with a null-forgiving operator.
        if (configuration.Name is not { Length: > 0 } serviceName)
            return GenericResult<AuthenticationSchemeBinding>.Failure(
                AuthenticationValidationLog.EntryMissingName(log, section.Path));
        if (configuration.Authority is not { Length: > 0 } authority)
            return GenericResult<AuthenticationSchemeBinding>.Failure(
                AuthenticationValidationLog.EntryMissingAuthority(log, serviceName));

        var typed = JwtBearerAuthenticationConfiguration.Read(section, serviceName, log);
        if (typed.IsFailure)
            return typed.ToNewResult<AuthenticationSchemeBinding>();
        if (typed.Value is not { } body)
            return GenericResult<AuthenticationSchemeBinding>.Failure(
                AuthenticationValidationLog.JwtBearerMissingAudience(log, serviceName));

        var schemeName = SchemeNameFor(serviceName);

        authenticationBuilder.AddJwtBearer(schemeName, options =>
        {
            options.Authority = authority;
            options.Audience = body.Audience;
            // Why the authority's own scheme decides this rather than a setting: an https authority can
            // and must have its metadata fetched over https, and an http one cannot. A knob here would
            // only let the two disagree. The header already proved the authority is absolute.
            options.RequireHttpsMetadata = string.Equals(
                new Uri(authority).Scheme, Uri.UriSchemeHttps, StringComparison.Ordinal);

            options.TokenValidationParameters.ValidateIssuer = true;
            options.TokenValidationParameters.ValidIssuer = authority;
            options.TokenValidationParameters.ValidateAudience = true;
            options.TokenValidationParameters.ValidAudience = body.Audience;
            options.TokenValidationParameters.ValidateLifetime = true;
            options.TokenValidationParameters.ValidateIssuerSigningKey = true;

            // Why the FDW claim names: everything downstream of authentication — User.IsInRole, the
            // permission pre-processor, ClaimsPrincipalAuthenticationContext — reads these, and a
            // principal built with the handler's defaults is invisible to all of them.
            options.TokenValidationParameters.RoleClaimType = ClaimDefinitions.roles.Name;
            options.TokenValidationParameters.NameClaimType = ClaimDefinitions.sub.Name;

            options.Events = new JwtBearerEvents
            {
                OnTokenValidated = context => ConferDeclaredRoles(context, serviceName, body.Roles),
            };
        });

        return GenericResult<AuthenticationSchemeBinding>.Success(
            new AuthenticationSchemeBinding(serviceName, authority, schemeName));
    }

    /// <summary>
    /// Builds the ASP.NET scheme name for an authentication service.
    /// </summary>
    /// <param name="serviceName">The service's declared name.</param>
    /// <returns>The scheme name.</returns>
    /// <remarks>
    /// Qualified by mechanism so two services of different mechanisms can share a name without one
    /// silently replacing the other's scheme options.
    /// </remarks>
    public static string SchemeNameFor(string serviceName) => "Fdw.JwtBearer." + serviceName;

    // Why the roles are put on the principal here rather than checked at each endpoint: the rest of the
    // framework authorizes off perm claims, so a principal without them is authenticated and powerless.
    // Baking them at validation is the same shape OpenIddict uses at issuance — the difference is only
    // that a remote issuer will not bake FDW's claims, so this side does it on the way in.
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

        // Why the resolver's own reason is what fails the request: it says whether the role does not
        // exist or the catalogue could not be read, and those want different fixes.
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
