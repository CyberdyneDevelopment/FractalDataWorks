using System;
using System.Collections.Generic;
using Fdw.Collections;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions;
using Fdw.Services.Authentication.Abstractions.Sessions;
using Fdw.Services.Authentication.Logging;
using Fdw.Services.Authentication.Sessions;
using Fdw.Services.Authentication.Sessions.Csrf;
using Fdw.Services.Data.Abstractions;
using Fdw.Web.Http.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Authentication.Validation;

/// <summary>
/// Authenticates requests by a server-side session: a random id in a cookie, resolved against
/// <c>auth.Session</c> on every request (FDW-903).
/// </summary>
/// <remarks>
/// <para>
/// The fourth mechanism beside <see cref="LocalKeyAuthenticationType"/>,
/// <see cref="JwtBearerAuthenticationType"/> and <see cref="ApiKeyAuthenticationType"/>. Like an API
/// key, the credential states nothing and everything the principal knows is read from the row it
/// matches. Unlike one, it arrives in a cookie, so the browser sends it unasked: every write it
/// authenticates passes the CSRF protection the service names.
/// </para>
/// <para>
/// A host that never declares a Session service loses nothing: bearer tokens stay a first-class way
/// in, and no cookie is ever read.
/// </para>
/// </remarks>
[Implementation(typeof(AuthenticationServiceTypes), "Session")]
public sealed class SessionAuthenticationType : AuthenticationServiceTypeBase
{
    /// <summary>The issuer a session is routed under.</summary>
    /// <remarks>
    /// A session id names no issuer; it is recognised by the cookie it arrives in. This host issued
    /// it, so this host is its issuer, and routing it through the same binding lookup as every other
    /// scheme keeps one routing path.
    /// </remarks>
    public const string SessionIssuer = "fdw:session";

    /// <summary>The prefix every Session scheme name carries.</summary>
    public const string SchemePrefix = "Fdw.Session.";

    /// <summary>Initializes a new instance of the <see cref="SessionAuthenticationType"/> class.</summary>
    public SessionAuthenticationType()
        : base("Session",
               "Server-side session",
               "Authenticates requests by a server-side session id carried in a cookie",
               defaultContainerName: "SessionAuthenticationServiceConfiguration")
    {
        Registration((builder, loggerFactory) =>
        {
            // Transient for the same reason the other handlers are: the handler holds the scheme and
            // the request it was initialised for in fields, so one instance per resolution.
            builder.Services.TryAddTransient<SessionAuthenticationHandler>();
            // The slide of a session's idle expiry is written at most once a minute (FDW-983); the same value is
            // stated where AuthenticationStepTypes registers the store, and TryAdd keeps whichever runs first.
            builder.Services.TryAddSingleton<ISessionStore>(sp =>
                ActivatorUtilities.CreateInstance<DatabaseSessionStore>(sp, TimeSpan.FromMinutes(1)));
            // Scoped because the revocation deny-list it consults is. The same registration is stated
            // where AuthenticationStepTypes registers the store; TryAdd keeps whichever body runs first.
            builder.Services.TryAddScoped<ISessionAuthenticator, SessionAuthenticator>();
            builder.Services.AddSingleton<ISessionAuthenticationConfigurationProvider, SessionAuthenticationConfigurationProvider>(sp => new SessionAuthenticationConfigurationProvider(sp.GetRequiredService<ILogger<SessionAuthenticationConfigurationProvider>>(), sp.GetRequiredService<IConfigurationGatewayProvider>(), AuthenticationServiceTypes.ConfigurationConnection, PathName, Container));

            // A session caller holds only a cookie, which means nothing to another host, so the outbound
            // API clients carry a token minted for the same principal instead (FDW-925). TryAdd, so a
            // provider registered before this one stands. A host whose outbound calls go as a service
            // identity of its own must register it with AddScoped after this phase (as the scheduler's
            // outbound identity does) or before it; a TryAdd after this phase is silently ignored.
            builder.Services.AddHttpContextAccessor();
            builder.Services.TryAddScoped<IAccessTokenProvider, ForwardedCallerTokenProvider>();
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });

        // Initialize, because the domain provider dispatches by the name registered here: without it a
        // row naming "Session" names a kind the registry has never heard of.
        Initialization((host, loggerFactory) =>
        {
            host.Services.GetRequiredService<IAuthenticationServiceConfigurationProvider>()
                .Register(Name, host.Services.GetRequiredService<ISessionAuthenticationConfigurationProvider>());
            return GenericResult<IHost>.Success(host);
        });
    }

    /// <summary>The scheme name this option registers for a given entry.</summary>
    /// <param name="serviceName">The declared entry's name.</param>
    /// <returns>The scheme name.</returns>
    public static string SchemeNameFor(string serviceName) => SchemePrefix + serviceName;

    /// <summary>Reads a Session service's SameSite mode, exactly as the row states it.</summary>
    /// <param name="configuration">The Session service's implementation row.</param>
    /// <param name="logger">The logger.</param>
    /// <returns>The mode, or a failure when the row names anything but Strict, Lax or None.</returns>
    public static IGenericResult<SameSiteMode> SameSiteOf(ISessionAuthenticationConfiguration configuration, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        return configuration.SameSite switch
        {
            "Strict" => GenericResult<SameSiteMode>.Success(SameSiteMode.Strict),
            "Lax" => GenericResult<SameSiteMode>.Success(SameSiteMode.Lax),
            "None" => GenericResult<SameSiteMode>.Success(SameSiteMode.None),
            _ => GenericResult<SameSiteMode>.Failure(SessionLog.SessionServiceInvalid(
                logger, configuration.Name, $"SameSite '{configuration.SameSite}' is not Strict, Lax or None")),
        };
    }

    /// <summary>Resolves the CSRF protection a Session service names.</summary>
    /// <param name="configuration">The Session service's implementation row.</param>
    /// <param name="logger">The logger.</param>
    /// <returns>The protection, or a failure when the row names one that is not registered.</returns>
    public static IGenericResult<ISessionCsrfProtection> CsrfProtectionOf(ISessionAuthenticationConfiguration configuration, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var protection = SessionCsrfProtectionTypes.ByName(configuration.CsrfProtection);
        return protection == SessionCsrfProtectionTypes.NotFound
            ? GenericResult<ISessionCsrfProtection>.Failure(SessionLog.SessionServiceInvalid(
                logger, configuration.Name, $"CsrfProtection '{configuration.CsrfProtection}' is not a registered protection (OriginCheck, SynchronizerToken)"))
            : GenericResult<ISessionCsrfProtection>.Success(protection);
    }

    /// <inheritdoc />
    public override string[] SupportedProtocols => ["Cookie"];

    /// <inheritdoc />
    public override string ProviderName => "Fdw.Services.Authentication";

    /// <inheritdoc />
    public override IReadOnlyList<string> SupportedFlows => ["AuthorizationCode", "Password"];

    /// <inheritdoc />
    public override IReadOnlyList<string> SupportedTokenTypes => [SessionRecord.TokenType];

    /// <inheritdoc />
    /// <remarks>
    /// Ahead of the token options: a session is recognised by its cookie without parsing anything,
    /// and only when no Authorization header is present.
    /// </remarks>
    public override int Priority => 4;

    /// <inheritdoc />
    /// <remarks>True: a session carries the tenant its sign-in landed in, exactly as a token would.</remarks>
    public override bool SupportsMultiTenant => true;

    /// <inheritdoc />
    public override bool SupportsTokenCaching => false;

    /// <inheritdoc />
    public override IGenericResult<AuthenticationSchemeBinding> TakeScheme(
        IAuthenticationServiceImplementationConfiguration configuration,
        IAuthenticationSchemeProvider schemes,
        IServiceProvider services,
        ILoggerFactory? loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(schemes);

        var log = loggerFactory?.CreateLogger<SessionAuthenticationType>()
            ?? NullLogger<SessionAuthenticationType>.Instance;

        if (configuration.Name is not { Length: > 0 } serviceName)
            return GenericResult<AuthenticationSchemeBinding>.Failure(
                AuthenticationValidationLog.EntryMissingName(log, "(unnamed)"));

        if (configuration is not ISessionAuthenticationConfiguration session)
            return GenericResult<AuthenticationSchemeBinding>.Failure(SessionLog.SessionServiceInvalid(
                log, serviceName, $"its row is a {configuration.GetType().Name}, not a Session implementation"));

        // Everything a request will need is checked now, so a misconfigured service stops the host at
        // startup instead of refusing its first signed-in user.
        if (string.IsNullOrWhiteSpace(session.CookieName))
            return GenericResult<AuthenticationSchemeBinding>.Failure(SessionLog.SessionServiceInvalid(
                log, serviceName, "it names no CookieName"));

        var sameSite = SameSiteOf(session, log);
        if (sameSite.IsFailure)
            return sameSite.ToNewResult<AuthenticationSchemeBinding>();

        var protection = CsrfProtectionOf(session, log);
        if (protection.IsFailure)
            return protection.ToNewResult<AuthenticationSchemeBinding>();

        var protectionValid = protection.Value!.ValidateConfiguration(session, log);
        if (protectionValid.IsFailure)
            return protectionValid.ToNewResult<AuthenticationSchemeBinding>();

        schemes.AddScheme(new AuthenticationScheme(
            SchemeNameFor(serviceName), displayName: null, handlerType: typeof(SessionAuthenticationHandler)));

        return GenericResult<AuthenticationSchemeBinding>.Success(
            new AuthenticationSchemeBinding(serviceName, SessionIssuer, SchemeNameFor(serviceName), session.CookieName));
    }
}
