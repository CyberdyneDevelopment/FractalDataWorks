using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Services;
using Fdw.Services.Abstractions;
using Fdw.ServiceTypes;
using Fdw.Services.ExternalIdentityProviders.Abstractions;
using Fdw.ServiceTypes.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;

namespace Fdw.Services.ExternalIdentityProviders;

/// <summary>
/// Collection of external identity provisioner service types. Structurally copies
/// <see cref="ExternalIdentityProviderTypes"/> and is swept by PlatformServices like every other
/// domain — no host registers it by hand. Config-selected: a (tenant, external provider) pair binds to
/// exactly one active <c>sec.ExternalIdentityProvisioner</c> row via
/// <c>ExternalIdentityProvisionerBindingConfigurationProvider</c>.
/// </summary>
[ExcludeFromCodeCoverage]
[ServiceTypeCollection(
    typeof(ExternalIdentityProvisionerTypeBase<IExternalIdentityProvisioner, ExternalIdentityProvisionerConfiguration, IExternalIdentityProvisionerFactory<IExternalIdentityProvisioner, ExternalIdentityProvisionerConfiguration>>),
    typeof(IExternalIdentityProvisionerType),
    typeof(ExternalIdentityProvisionerTypes),
    GenerateProvider = true,
    ServiceInterface = typeof(IExternalIdentityProvisioner),
    ConfigurationType = typeof(ExternalIdentityProvisionerConfiguration),
    // Why: DefaultExternalIdentityProvisionerProvider (not the raw DefaultServiceProvider) so the
    // provider supplies ITSELF to the factory at Create time. That keeps provisioner factories pure —
    // a factory that ctor-injected this provider recursed forever during the provider's own
    // realization and hung the host silently (FDW-615).
    ProviderType = typeof(DefaultExternalIdentityProvisionerProvider),
    ProviderInterface = typeof(IFdwServiceProvider<IExternalIdentityProvisioner, ExternalIdentityProvisionerConfiguration>),
    ServiceCategory = "ExternalIdentityProvisioner")]
public partial class ExternalIdentityProvisionerTypes : ServiceTypeCollectionBase<
    ExternalIdentityProvisionerTypeBase<IExternalIdentityProvisioner, ExternalIdentityProvisionerConfiguration, IExternalIdentityProvisionerFactory<IExternalIdentityProvisioner, ExternalIdentityProvisionerConfiguration>>,
    IExternalIdentityProvisionerType>
{
    // Configure(), Register(), Initialize() are source-generated.

    /// <summary>
    /// Sets this collection's Register body: the option sweep, then this domain's provider.
    /// </summary>
    /// <remarks>
    /// The provider is one registration for the whole collection and this declaration already names it,
    /// so the body that registers it is written here beside the declaration. Setting it as the phase's
    /// body is what makes it replaceable: an application calling <c>Registration(...)</c> replaces the
    /// sweep and this registration together, which is the correct semantic for a host taking over phase 2.
    /// </remarks>
    static ExternalIdentityProvisionerTypes()
    {
        var sweepOptions = RegisterFunc;
        Registration((builder, loggerFactory) =>
        {
            sweepOptions(builder, loggerFactory);
            builder.Services.AddScoped<IFdwServiceProvider<IExternalIdentityProvisioner, ExternalIdentityProvisionerConfiguration>>(sp =>
            {
                var provider = new DefaultExternalIdentityProvisionerProvider(
                    sp,
                    sp.GetService<ILoggerFactory>()?.CreateLogger<DefaultExternalIdentityProvisionerProvider>()
                    ?? NullLogger<DefaultExternalIdentityProvisionerProvider>.Instance);
                var stLogger = sp.GetService<ILoggerFactory>()?.CreateLogger("ExternalIdentityProvisionerTypes");
                try
                {
                    if (sp.GetService<IServiceConfigurationProvider<ExternalIdentityProvisionerConfiguration>>() is { } cfgProvider)
                    {
                        // Why the result is read: a provider that did not take its parent still constructs, and
                        // every later read silently misses. The failure has to be said out loud here or nowhere.
                        var parentResult = provider.RegisterParentProvider(cfgProvider);
                        if (!parentResult.IsSuccess && stLogger != null)
                            ServiceTypeLog.FactoryRegistrationFailed(stLogger, "ExternalIdentityProvisionerTypes", parentResult.CurrentMessage ?? "ExternalIdentityProvisionerTypes");
                    }
                }
                catch (Exception ex)
                {
                    // Why rethrow: a throw here was previously silent, and a provider that failed to take
                    // its parent is unusable in a way that only surfaces much later.
                    if (stLogger != null) ServiceTypeLog.FactoryRegistrationException(stLogger, ex, "ExternalIdentityProvisionerTypes");
                    throw;
                }
                return provider;
            });
            return builder;
        });
    }
}
