using System;
using System.Collections.Generic;
using Fdw.Collections;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions;
using Fdw.Services.Authentication.Logging;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Authentication.Validation;

/// <summary>
/// Validates the opaque credentials this host mints itself: agent keys and personal access tokens.
/// </summary>
/// <remarks>
/// <para>
/// The third mechanism beside <see cref="LocalKeyAuthenticationType"/> and
/// <see cref="JwtBearerAuthenticationType"/>, and the one the seed data has always named:
/// <c>auth.Authentication</c> carries an <c>ApiKey</c> row alongside <c>ApiJwtAuth</c> and
/// <c>AuthentikSso</c>, and until now nothing implemented it.
/// </para>
/// <para>
/// It belongs here rather than in the login flow because of what the credential IS. An
/// login-flow step runs once at login and ends by minting a token; even
/// the steps that accept an external credential exchange it for a fresh one. An agent key is the
/// opposite shape — long-lived, presented directly on every request, with nothing to issue and
/// nothing to exchange. Validating an inbound credential per request is what this collection is.
/// </para>
/// </remarks>
[ServiceTypeOption(typeof(AuthenticationServiceTypes), "ApiKey")]
public sealed class ApiKeyAuthenticationType : AuthenticationServiceTypeBase
{
    /// <summary>The bearer prefix every credential of this kind carries.</summary>
    public const string CredentialPrefix = "Bearer fdx_";

    /// <summary>The bearer prefix that marks an agent key specifically.</summary>
    /// <remarks>
    /// Agent keys and personal access tokens are minted by the same generator; the environment
    /// segment is what separates them, and <c>agent</c> is reserved for keys minted by
    /// <c>IAgentKeyService</c>.
    /// </remarks>
    public const string AgentKeyPrefix = "Bearer fdx_agent_";

    /// <summary>The issuer an opaque credential is routed by.</summary>
    /// <remarks>
    /// A bearer token names its issuer and the selector reads it. An opaque credential names
    /// nothing — it is a lookup key — so it has no issuer to read, and the selector recognises it by
    /// prefix and routes it under this well-known value instead.
    ///
    /// It is a real issuer rather than a sentinel: this host mints these credentials, so this host
    /// is who issued them. Routing them through the same binding lookup as every other scheme is
    /// deliberate — a branch that bypassed the bindings would be a second routing path to keep in
    /// step with the first.
    /// </remarks>
    public const string OpaqueCredentialIssuer = "fdw:opaque-credential";

    /// <summary>The prefix every ApiKey scheme name carries.</summary>
    public const string SchemePrefix = "Fdw.ApiKey.";

    /// <summary>Initializes a new instance of the <see cref="ApiKeyAuthenticationType"/> class.</summary>
    public ApiKeyAuthenticationType()
        : base("ApiKey",
               "API Key",
               "Validates agent keys and personal access tokens this host minted")
    {
        Registration((builder, loggerFactory) =>
        {
            // Transient for the same reason the other handlers are: the handler holds the scheme and
            // the request it was initialised for in fields, so one instance per resolution.
            builder.Services.TryAddTransient<ApiKeyAuthenticationHandler>();
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });
    }

    /// <summary>The scheme name this option registers for a given entry.</summary>
    /// <param name="serviceName">The declared entry's name.</param>
    /// <returns>The scheme name.</returns>
    public static string SchemeNameFor(string serviceName) => SchemePrefix + serviceName;

    /// <inheritdoc />
    public override string[] SupportedProtocols => ["ApiKey"];

    /// <inheritdoc />
    public override string ProviderName => "Fdw.Services.Authentication";

    /// <inheritdoc />
    public override IReadOnlyList<string> SupportedFlows => ["ClientCredentials"];

    /// <inheritdoc />
    public override IReadOnlyList<string> SupportedTokenTypes => ["AgentKey", "PersonalAccessToken"];

    /// <inheritdoc />
    /// <remarks>
    /// Ahead of the token options: an opaque credential is recognised by its prefix without parsing
    /// anything, so trying it first costs nothing when it does not match.
    /// </remarks>
    public override int Priority => 5;

    /// <inheritdoc />
    /// <remarks>
    /// False: the credential carries no tenant. It is bound to a user at creation time and nothing
    /// in it names a tenant to operate under, so the permissions it resolves are the untenanted set.
    /// </remarks>
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
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(schemes);

        var log = loggerFactory?.CreateLogger<ApiKeyAuthenticationType>()
            ?? NullLogger<ApiKeyAuthenticationType>.Instance;

        if (configuration.Name is not { Length: > 0 } serviceName)
        {
            return GenericResult<AuthenticationSchemeBinding>.Failure(
                AuthenticationValidationLog.EntryMissingName(log, "(unnamed)"));
        }

        // No Authority is read from the row. The other options need one because a token names its
        // issuer and the scheme must match it; these credentials name nothing, so a row carrying an
        // authority for this kind would be describing something that is never consulted.
        schemes.AddScheme(new AuthenticationScheme(
            SchemeNameFor(serviceName), displayName: null, handlerType: typeof(ApiKeyAuthenticationHandler)));

        return GenericResult<AuthenticationSchemeBinding>.Success(
            new AuthenticationSchemeBinding(serviceName, OpaqueCredentialIssuer, SchemeNameFor(serviceName)));
    }
}
