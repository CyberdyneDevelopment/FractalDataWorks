namespace Fdw.Web.Http.Authentication.OpenIdConnect;

using System;
using Fdw.Services.Authentication.Clients;
using Fdw.Web.Http.Authentication.OpenIdConnect.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

/// <summary>
/// Extension methods for registering OpenID Connect authentication services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers OIDC authentication services with the specified provider options.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">An action to configure the OIDC provider options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddOidcAuthentication(
        this IServiceCollection services,
        Action<OidcProviderOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(configureOptions);

        services.Configure(configureOptions);
        services.TryAddScoped<IForgotPasswordProvider, OidcForgotPasswordProvider>();

        return services;
    }

    /// <summary>
    /// Registers OIDC authentication services pre-configured for Authentik.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="authority">The Authentik authority URL.</param>
    /// <param name="clientId">The client ID registered in Authentik.</param>
    /// <param name="clientSecret">The client secret, if applicable.</param>
    /// <param name="loggerFactory">Optional logger factory for registration logging.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddAuthentikAuthentication(
        this IServiceCollection services,
        string authority,
        string clientId,
        string? clientSecret = null,
        ILoggerFactory? loggerFactory = null)
    {
        ArgumentNullException.ThrowIfNull(authority);
        ArgumentNullException.ThrowIfNull(clientId);

        var logger = loggerFactory?.CreateLogger(typeof(ServiceCollectionExtensions).FullName!);

        services.AddOidcAuthentication(options =>
        {
            options.Authority = authority;
            options.ClientId = clientId;
            options.ClientSecret = clientSecret;
            options.CallbackPath = AuthentikDefaults.CallbackPath;
            options.SignedOutCallbackPath = AuthentikDefaults.SignedOutCallbackPath;
            options.DisplayName = AuthentikDefaults.DisplayName;
            options.PasswordRecoveryUrlTemplate = AuthentikDefaults.GetPasswordRecoveryUrl(authority);

            if (logger is not null)
            {
                OidcAuthLog.AuthentikRegistered(logger, authority);
            }
        });

        return services;
    }
}
