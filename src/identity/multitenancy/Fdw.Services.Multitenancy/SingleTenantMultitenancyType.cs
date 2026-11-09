using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Services.Data.Abstractions;
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
/// consumers which always inject these interfaces (e.g. Tenant admin endpoints) resolve without a
/// DI failure — every query simply
/// reports "no tenant"/"no organization" rather than crashing the host.
/// </summary>
[ExcludeFromCodeCoverage]
[Implementation(typeof(MultitenancyTypes), "SingleTenant")]
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

        Registration((builder, loggerFactory) =>
        {

            RegisterAlwaysOnContexts(builder.Services);

            builder.Services.AddScoped<ITenantProvider, NullTenantProvider>();
            // A real read, not the null provider (FDW-4573): this host resolves no tenant per request, but a pipeline is made in an organization, and the check that the
            // organization exists, belongs to a tenant and is active has to find it. The SQL multitenancy type registers its own provider for hosts that resolve tenants.
            builder.Services.TryAddSingleton<IOrganizationProvider>(sp => new ConfiguredOrganizationProvider(
                sp.GetRequiredService<IConfigurationGatewayProvider>(),
                MultitenancyTypes.ConfigurationConnection, "tenant", "Organization",
                sp.GetService<ILogger<ConfiguredOrganizationProvider>>()));

            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });

    }

    /// <inheritdoc/>
    public override bool EnablesTenantResolution => false;

}
