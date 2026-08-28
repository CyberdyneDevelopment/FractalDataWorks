using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.ServiceTypes;
using Fdw.Services.ExternalIdentityProviders.Abstractions;
using Fdw.Services.ExternalIdentityProviders.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.ExternalIdentityProviders;

/// <summary>
/// Resolves the <see cref="IExternalIdentityProvider"/> a caller means by an optional explicit
/// <c>provider</c> name — falling back to "the one active configuration" ONLY when exactly one exists.
/// Never guesses among multiple active providers. Mirrors
/// <c>Fdw.Services.TokenManagers.AuthenticationService.ResolveActiveTokenManager</c>'s
/// read-headers-then-resolve-by-name shape, generalized with an optional caller-supplied name (since,
/// unlike TokenManagers, this domain is not a "declared choice" — several configurations may be active
/// at once).
/// </summary>
public sealed class ExternalIdentityProviderResolver
{
    private readonly IPlatformServiceProvider<IExternalIdentityProvider, IExternalIdentityProviderImplementationConfiguration> _provider;
    private readonly ExternalIdentityProviderConfigurationProvider _configurationProvider;
    private readonly ILogger<ExternalIdentityProviderResolver> _logger;

    /// <summary>
    /// Registers <see cref="ExternalIdentityProviderResolver"/> with DI. Idempotent — safe to call from
    /// any consumer's registration cascade (e.g. the OpenIddict token endpoint's own service type option).
    /// </summary>
    public static void RegisterDomainServices(IServiceCollection services)
    {
        services.TryAddScoped<ExternalIdentityProviderResolver>();
    }

    /// <summary>Initializes a new instance of the <see cref="ExternalIdentityProviderResolver"/> class.</summary>
    public ExternalIdentityProviderResolver(
        IPlatformServiceProvider<IExternalIdentityProvider, IExternalIdentityProviderImplementationConfiguration> provider,
        ExternalIdentityProviderConfigurationProvider configurationProvider,
        ILogger<ExternalIdentityProviderResolver>? logger)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        _configurationProvider = configurationProvider ?? throw new ArgumentNullException(nameof(configurationProvider));
        _logger = logger ?? NullLogger<ExternalIdentityProviderResolver>.Instance;
    }

    /// <summary>
    /// Resolves an <see cref="IExternalIdentityProvider"/> by <paramref name="providerName"/> when
    /// supplied; otherwise resolves the single active configuration. Fails loud — never a fallback —
    /// when the named provider can't be found, no configuration is active, or more than one is active
    /// without a name to disambiguate.
    /// </summary>
    /// <param name="providerName">The caller-supplied provider name (e.g. the <c>provider</c> token form parameter), or null/empty to resolve implicitly.</param>
    /// <param name="cancellationToken">Propagated cancellation token.</param>
    public Task<IGenericResult<IExternalIdentityProvider>> Resolve(string? providerName, CancellationToken cancellationToken = default)
    {
        ExternalIdentityProviderLog.ResolvingProvider(_logger, providerName ?? "(none)");

        return string.IsNullOrEmpty(providerName)
            ? ResolveImplicit(cancellationToken)
            : ResolveByName(providerName, cancellationToken);
    }

    private async Task<IGenericResult<IExternalIdentityProvider>> ResolveByName(string providerName, CancellationToken cancellationToken)
    {
        var byNameResult = await _provider.Get(providerName, cancellationToken).ConfigureAwait(false);
        if (!byNameResult.IsSuccess || byNameResult.Value is null)
            return GenericResult<IExternalIdentityProvider>.Failure(
                ExternalIdentityProviderLog.ExternalIdentityProviderNotConfigured(_logger,
                    $"provider '{providerName}' does not resolve to an active ExternalIdentityProvider configuration."));

        ExternalIdentityProviderLog.ProviderResolved(_logger, providerName);
        return byNameResult;
    }

    private async Task<IGenericResult<IExternalIdentityProvider>> ResolveImplicit(CancellationToken cancellationToken)
    {
        var headersResult = await _configurationProvider.Get(cancellationToken).ConfigureAwait(false);
        if (!headersResult.IsSuccess)
            return headersResult.ToNewResult<IExternalIdentityProvider>();

        var headers = headersResult.Value;
        if (headers is null || headers.Count == 0)
            return GenericResult<IExternalIdentityProvider>.Failure(
                ExternalIdentityProviderLog.ExternalIdentityProviderNotConfigured(_logger,
                    "no active ExternalIdentityProvider configurations exist."));

        if (headers.Count > 1)
            return GenericResult<IExternalIdentityProvider>.Failure(
                ExternalIdentityProviderLog.ExternalIdentityProviderNotConfigured(_logger,
                    $"{headers.Count} active ExternalIdentityProvider configurations exist; a 'provider' parameter is required."));

        var resolvedResult = await _provider.Get(headers[0].Name, cancellationToken).ConfigureAwait(false);
        if (resolvedResult.IsSuccess)
            ExternalIdentityProviderLog.ProviderResolved(_logger, headers[0].Name);
        return resolvedResult;
    }
}
