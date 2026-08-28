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

        Registration((builder, loggerFactory) =>
        {

            RegisterAlwaysOnContexts(builder.Services);

            builder.Services.AddScoped<ITenantProvider, NullTenantProvider>();
            builder.Services.TryAddSingleton<IOrganizationProvider>(_ => NullOrganizationProvider.Instance);

            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });

    }

    /// <inheritdoc/>
    public override bool EnablesTenantResolution => false;

}
