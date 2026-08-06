namespace Fdw.Web.Http.Authentication.OpenIdConnect;

using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Services.Authentication.Clients;
using Fdw.Services.Authentication.Clients.Models;
using Fdw.Web.Http.Authentication.OpenIdConnect.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>
/// Implements forgot-password for OIDC providers by returning a redirect URL
/// to the provider's account recovery flow.
/// </summary>
public sealed class OidcForgotPasswordProvider : IForgotPasswordProvider
{
    private readonly OidcProviderOptions _options;
    private readonly ILogger<OidcForgotPasswordProvider> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="OidcForgotPasswordProvider"/> class.
    /// </summary>
    /// <param name="options">The OIDC provider options.</param>
    /// <param name="logger">The logger.</param>
    public OidcForgotPasswordProvider(
        IOptions<OidcProviderOptions> options,
        ILogger<OidcForgotPasswordProvider> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<ForgotPasswordResult> RequestPasswordReset(string identifier, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(identifier);

        var recoveryUrl = _options.PasswordRecoveryUrlTemplate;

        if (string.IsNullOrEmpty(recoveryUrl))
        {
            OidcAuthLog.NoRecoveryUrlConfigured(_logger, _options.DisplayName);
            return Task.FromResult(
                ForgotPasswordResult.Failed($"Password recovery is not configured for {_options.DisplayName}."));
        }

        var resolvedUrl = recoveryUrl.Replace("{authority}", _options.Authority.TrimEnd('/'), StringComparison.OrdinalIgnoreCase);

        OidcAuthLog.RedirectingToRecovery(_logger, _options.DisplayName, resolvedUrl);
        return Task.FromResult(ForgotPasswordResult.Redirect(resolvedUrl));
    }
}
