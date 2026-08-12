using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Services.Multitenancy.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Fdw.Results;

namespace Fdw.Services.Multitenancy;

/// <summary>
/// Multitenancy option for hosts that run a single tenant — the explicit "no real tenant store"
/// choice. Registers only the always-on request-scoped contexts and the null-object
/// <see cref="ITenantProvider"/>/<see cref="IOrganizationProvider"/> implementations so that
/// consumers which always inject these interfaces (e.g. Tenant admin endpoints,
/// <c>DefaultPrincipalResolver</c> during sign-in) resolve without a DI failure — every query simply
/// reports "no tenant"/"no organization" rather than crashing the host.
/// </summary>
[ExcludeFromCodeCoverage]
[ServiceTypeOption(typeof(MultitenancyTypes), "SingleTenant")]
public sealed class SingleTenantMultitenancyType : MultitenancyTypeBase<ISingleTenantMultitenancyFactory>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SingleTenantMultitenancyType"/> class.
    /// </summary>
    public SingleTenantMultitenancyType() : base(
        name: "SingleTenant",
        sectionName: "SingleTenant",
        displayName: "Single Tenant",
        description: "No real tenant store — request-scoped contexts only, tenant/org resolution disabled")
    {
        // Why no Configuration phase: this option used to bind a "Multitenancy" IConfiguration section
        // into List<MultitenancyConfiguration> so a host could declare ServiceOptionType via appsettings
        // or Multitenancy__0__ServiceOptionType. Nothing ever read that value -- the active option is
        // selected from ConfigurationSchema.Multitenancy (configurationSchema.json) -- so the binding
        // was a lever that looked live and was not.

        Registration((builder, loggerFactory) =>
        {

            RegisterAlwaysOnContexts(builder.Services);

            // Why: register null-object providers so endpoints/builder.Services that always inject
            // ITenantProvider/IOrganizationProvider (Tenant admin endpoints, DefaultPrincipalResolver at
            // sign-in) do not crash with DI resolution failures when this host has no real tenant store.
            // Consumers guard via IsSuccess/HasTenant/HasOrg.
            builder.Services.AddScoped<ITenantProvider, NullTenantProvider>();
            builder.Services.TryAddSingleton<IOrganizationProvider>(_ => NullOrganizationProvider.Instance);

            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });

    }

    /// <inheritdoc/>
    public override bool EnablesTenantResolution => false;

}
