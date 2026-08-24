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
using System.Linq;
using Fdw.Results;
using Microsoft.Extensions.Hosting;

namespace Fdw.Services.ExternalIdentityProviders;

/// <summary>
/// Collection of external identity provisioner service types. Structurally copies
/// <see cref="ExternalIdentityProviderTypes"/> and is collected by PlatformServices like every other
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
    ProviderInterface = typeof(IPlatformServiceProvider<IExternalIdentityProvisioner, ExternalIdentityProvisionerConfiguration>),
    ServiceCategory = "ExternalIdentityProvisioner")]
public partial class ExternalIdentityProvisionerTypes : ServiceTypeCollectionBase<
    ExternalIdentityProvisionerTypeBase<IExternalIdentityProvisioner, ExternalIdentityProvisionerConfiguration, IExternalIdentityProvisionerFactory<IExternalIdentityProvisioner, ExternalIdentityProvisionerConfiguration>>,
    IExternalIdentityProvisionerType>
{
    // Configure(), Register(), Initialize() are source-generated.

    /// <summary>
    /// Sets this collection's Register body: the option collect, then this domain's provider.
    /// </summary>
    /// <remarks>
    /// The provider is one registration for the whole collection and this declaration already names it,
    /// so the body that registers it is written here beside the declaration. Setting it as the phase's
    /// body is what makes it replaceable: an application calling <c>Registration(...)</c> replaces the
    /// collect and this registration together, which is the correct semantic for a host taking over phase 2.
    /// </remarks>
    static ExternalIdentityProvisionerTypes()
    {
        var collectOptions = RegisterFunc;

        // Why a local: this closed generic is the DI key a consumer injects, and it is reported at
        // three points below — the deferred declaration, the milestone, and the zero-option warning.
        // Written out three times it is three chances for them to disagree.
        var providerService = typeof(IPlatformServiceProvider<IExternalIdentityProvisioner, ExternalIdentityProvisionerConfiguration>).ToString();

        Registration((builder, loggerFactory) =>
        {
            var log = loggerFactory?.CreateLogger<ExternalIdentityProvisionerTypes>() ?? NullLogger<ExternalIdentityProvisionerTypes>.Instance;

            // Why the result is read: this replacement calls the func it captured, and discarding
            // what that returned meant an option that failed to register was followed by this body
            // registering the provider anyway and reporting success.
            var registered = collectOptions(builder, loggerFactory);
            if (registered.IsFailure)
                return registered;

            var declaredOptions = Options;
            var optionNames = string.Join(", ", declaredOptions.Select(option => option.Name));

            ServiceTypeLog.DomainOptionsCollected(log, nameof(ExternalIdentityProvisionerTypes), declaredOptions.Length, optionNames);
            ServiceTypeLog.DomainProviderDeclared(log, nameof(ExternalIdentityProvisionerTypes), providerService);

            builder.Services.AddScoped<IPlatformServiceProvider<IExternalIdentityProvisioner, ExternalIdentityProvisionerConfiguration>>(sp =>
            {
                var provider = new DefaultExternalIdentityProvisionerProvider(
                    sp,
                    sp.GetService<ILoggerFactory>()?.CreateLogger<DefaultExternalIdentityProvisionerProvider>()
                    ?? NullLogger<DefaultExternalIdentityProvisionerProvider>.Instance);

                // Why ILogger<ExternalIdentityProvisionerTypes> and not CreateLogger("ExternalIdentityProvisionerTypes"): SourceContext then
                // carries the namespace-qualified collection, and the category cannot drift from the
                // type it claims to name. The provider logs its own lines under its own type, so the
                // two layers read base-then-derived rather than collapsing onto one category.
                var stLogger = sp.GetService<ILoggerFactory>()?.CreateLogger<ExternalIdentityProvisionerTypes>()
                    ?? NullLogger<ExternalIdentityProvisionerTypes>.Instance;
                ServiceTypeLog.DomainProviderConstructing(stLogger, nameof(ExternalIdentityProvisionerTypes), provider.GetType().Name);
                try
                {
                    if (sp.GetService<IServiceConfigurationProvider<ExternalIdentityProvisionerConfiguration>>() is { } cfgProvider)
                    {
                        // Why the result is read: a provider that did not take its parent still constructs, and
                        // every later read silently misses. The failure has to be said out loud here or nowhere.
                        var parentResult = provider.Register(cfgProvider);
                        if (parentResult.IsSuccess)
                            ServiceTypeLog.DomainConfigurationSourceAttached(stLogger, nameof(ExternalIdentityProvisionerTypes), provider.GetType().Name, cfgProvider.GetType().Name);
                        else
                            ServiceTypeLog.DomainConfigurationSourceRejected(stLogger, nameof(ExternalIdentityProvisionerTypes), provider.GetType().Name, cfgProvider.GetType().Name, parentResult.CurrentMessage);
                    }
                    else
                    {
                        // Why Critical, and why the collection says it rather than the provider: from
                        // inside the provider a null parent is indistinguishable from a domain that needs
                        // none. This is the one place that knows one was meant to arrive, and without it
                        // the domain fails every lookup by name for the life of the scope with nothing
                        // pointing back here.
                        ServiceTypeLog.DomainHasNoConfigurationSource(
                            stLogger,
                            nameof(ExternalIdentityProvisionerTypes),
                            provider.GetType().Name,
                            typeof(IServiceConfigurationProvider<ExternalIdentityProvisionerConfiguration>).ToString());
                    }
                }
                catch (Exception ex)
                {
                    // Why rethrow: a throw here was previously silent, and a provider that failed to take
                    // its parent is unusable in a way that only surfaces much later.
                    ServiceTypeLog.FactoryRegistrationException(stLogger, ex, nameof(ExternalIdentityProvisionerTypes));
                    throw;
                }
                return provider;
            });

            // Why the milestone comes after the registration and not before: it states that the domain
            // finished phase 2, which is only true once the provider is actually in the container.
            if (declaredOptions.Length == 0)
                ServiceTypeLog.DomainRegisteredWithNoOptions(log, nameof(ExternalIdentityProvisionerTypes), providerService);
            else
                ServiceTypeLog.DomainRegistered(log, nameof(ExternalIdentityProvisionerTypes), declaredOptions.Length, optionNames, providerService);

            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });
    }
}
