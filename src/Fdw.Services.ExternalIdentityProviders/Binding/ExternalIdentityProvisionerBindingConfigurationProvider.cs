using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.ExternalIdentityProviders.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Fdw.Services.ExternalIdentityProviders.Abstractions;

namespace Fdw.Services.ExternalIdentityProviders.Binding;

/// <summary>
/// Configuration provider for ExternalIdentityProvisionerBindingConfiguration rows in
/// sec.ExternalIdentityProvisionerBinding. Reads through IConfigurationGateway — no IConfiguration
/// binding section. Adds <see cref="ResolveProvisionerName"/>, the single selector callers (e.g.
/// <c>OpenIdTokenManager</c>) use to pick a named provisioner for a (tenant, external provider) pair.
/// </summary>
public class ExternalIdentityProvisionerBindingConfigurationProvider
    : ImplementationConfigurationProviderBase<ExternalIdentityProvisionerBindingConfiguration, ExternalIdentityProvisionerBindingConfigurationCommand>
{
    private readonly ILogger _logger;

    /// <summary>
    /// Registers the ExternalIdentityProvisionerBindingConfigurationProvider and interface forwardings
    /// with DI, targeting this domain's own default location. To override, call
    /// <c>SetConfiguration</c> on the resolved singleton.
    /// </summary>
    public static void RegisterDomainServices(IServiceCollection services)
    {
        services.TryAddSingleton<ExternalIdentityProvisionerBindingConfigurationProvider>(sp =>
            new ExternalIdentityProvisionerBindingConfigurationProvider(
                sp.GetService<ILogger<ExternalIdentityProvisionerBindingConfigurationProvider>>()!,
                sp.GetRequiredService<IConfigurationGatewayProvider>()));

        services.TryAddSingleton<ImplementationConfigurationProviderBase<ExternalIdentityProvisionerBindingConfiguration, ExternalIdentityProvisionerBindingConfigurationCommand>>(
            sp => sp.GetRequiredService<ExternalIdentityProvisionerBindingConfigurationProvider>());

        services.TryAddSingleton<IServiceConfigurationProvider<ExternalIdentityProvisionerBindingConfiguration>>(sp =>
            sp.GetRequiredService<ExternalIdentityProvisionerBindingConfigurationProvider>());
    }

    /// <summary>Initializes a new instance of the <see cref="ExternalIdentityProvisionerBindingConfigurationProvider"/> class.</summary>
    public ExternalIdentityProvisionerBindingConfigurationProvider(
        ILogger<ExternalIdentityProvisionerBindingConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName = "ConfigurationDb",
        string pathName = "sec")
        : base(logger ?? NullLogger<ExternalIdentityProvisionerBindingConfigurationProvider>.Instance,
               gatewayProvider,
               dataStoreName, pathName)
    {
        // Why: the base class's own ILogger field is private — this provider's extra
        // ResolveProvisionerName method needs its own reference to the same logger for its
        // domain-specific MessageLogging calls.
        _logger = logger ?? NullLogger<ExternalIdentityProvisionerBindingConfigurationProvider>.Instance;
    }

    /// <summary>
    /// Resolves the name of the provisioner bound to the EXACT (<paramref name="tenantId"/>,
    /// <paramref name="providerName"/>) pair. No tenant-to-global fall-through: a tenant-scoped lookup
    /// that finds no row does NOT retry against the global (<c>TenantId == null</c>) binding — the
    /// caller must supply <c>tenantId: null</c> explicitly to resolve the global binding. Zero matches
    /// is a legitimate default-OFF outcome (<c>Success(null)</c>), not a failure; more than one current
    /// match for the same pair is an ambiguity and fails loud.
    /// </summary>
    /// <param name="tenantId">The tenant to match exactly, or null for the global binding.</param>
    /// <param name="providerName">The external identity provider name to match (case-insensitive).</param>
    /// <param name="cancellationToken">Propagated cancellation token.</param>
    public async Task<IGenericResult<string?>> ResolveProvisionerName(
        Guid? tenantId, string providerName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(providerName);

        var tenantLabel = tenantId?.ToString() ?? "(global)";
        ExternalIdentityProvisionerLog.ResolvingBinding(_logger, tenantLabel, providerName);

        var allResult = await Get(cancellationToken).ConfigureAwait(false);
        if (!allResult.IsSuccess)
            return GenericResult<string?>.Failure(
                ExternalIdentityProvisionerLog.BindingReadFailed(
                    _logger, tenantLabel, providerName, allResult.CurrentMessage ?? "binding read failed."));

        // Why: EXACT equality only, including null==null for the global row — no tenant->global
        // fall-through. That would be a forbidden implicit fallback (see NO FALLBACKS WITHOUT EXPLICIT
        // APPROVAL): a caller that wants the global binding must pass tenantId: null itself.
        var matches = (allResult.Value ?? [])
            .Where(b => b.TenantId == tenantId
                && string.Equals(b.ProviderName, providerName, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (matches.Count == 0)
        {
            ExternalIdentityProvisionerLog.BindingAbsent(_logger, tenantLabel, providerName);
            return GenericResult<string?>.Success(null);
        }

        if (matches.Count > 1)
            return GenericResult<string?>.Failure(
                ExternalIdentityProvisionerLog.BindingAmbiguous(_logger, matches.Count, tenantLabel, providerName));

        ExternalIdentityProvisionerLog.BindingResolved(_logger, tenantLabel, providerName, matches[0].ProvisionerName);
        return GenericResult<string?>.Success(matches[0].ProvisionerName);
    }
}
