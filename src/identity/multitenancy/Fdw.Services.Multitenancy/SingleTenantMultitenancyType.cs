using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Messages;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Configuration;
using Fdw.Services.Multitenancy.Logging;
using Fdw.Services.Multitenancy.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Fdw.Results;

namespace Fdw.Services.Multitenancy;

/// <summary>
/// Multitenancy option for hosts that resolve no tenant from a request (the ETL host, the scheduler). Registers the always-on request-scoped contexts and
/// <see cref="ConfiguredTenantProvider"/>/<see cref="ConfiguredOrganizationProvider"/>, which answer tenant and organization facts from the configuration database
/// (<c>tenant.Tenants</c>, <c>tenant.UserTenantsConfiguration</c>, <c>tenant.Organization</c>), so the check that a person may act in a tenant, and that a tenant
/// is the global one, can be made (FDW-6528). It resolves no tenant per request: <see cref="EnablesTenantResolution"/> is false.
/// </summary>
/// <remarks>
/// The name is a poor fit for the hosts that use it: they act for whichever tenant a run or a schedule names, so they are not "single tenant" in the sense the
/// name says. It is kept so no host file changes (FDW-6528); whoever next touches multitenancy should name what the option does.
/// </remarks>
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
        description: "Answers tenant and organization facts from the configuration database and resolves no tenant from a request")
    {

        Registration((builder, loggerFactory) =>
        {

            RegisterAlwaysOnContexts(builder.Services);

            // A real read (FDW-6528): a run started for a person is judged by whether that person is a current member of the tenant it acts in and whether the tenant
            // is the global one, and the null provider answered "unavailable" to both, so every such run was refused on the ETL host and the scheduler.
            builder.Services.AddScoped<ITenantProvider>(sp => new ConfiguredTenantProvider(
                sp.GetRequiredService<IConfigurationGatewayProvider>(),
                MultitenancyTypes.ConfigurationConnection, TenantPath, TenantsContainer, UserTenantsContainer,
                sp.GetService<ILogger<ConfiguredTenantProvider>>()));
            // A real read, not the null provider (FDW-4573): this host resolves no tenant per request, but a pipeline is made in an organization, and the check that the
            // organization exists, belongs to a tenant and is active has to find it. The SQL multitenancy type registers its own provider for hosts that resolve tenants.
            builder.Services.TryAddSingleton<IOrganizationProvider>(sp => new ConfiguredOrganizationProvider(
                sp.GetRequiredService<IConfigurationGatewayProvider>(),
                MultitenancyTypes.ConfigurationConnection, TenantPath, OrganizationContainer,
                sp.GetService<ILogger<ConfiguredOrganizationProvider>>()));

            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });

        // Every container the option reads has to be declared, and a gap stops the host with the container named, in one pass (FDW-6528). The tenant lookup reads
        // them when a run is judged, so a host whose schema lacks one would otherwise start and refuse every run for a person.
        Initialization((host, loggerFactory) =>
        {
            var logger = loggerFactory?.CreateLogger<SingleTenantMultitenancyType>() ?? NullLogger<SingleTenantMultitenancyType>.Instance;
            var declared = host.Services.GetRequiredService<ConfigurationSchema>().DataStores
                .Where(store => string.Equals(store.Name, MultitenancyTypes.ConfigurationConnection, StringComparison.OrdinalIgnoreCase))
                .SelectMany(store => store.Paths)
                .Where(path => string.Equals(path.Name, TenantPath, StringComparison.OrdinalIgnoreCase))
                .SelectMany(path => path.Containers)
                .Select(container => container.Name)
                .ToList();
            List<IGenericMessage> gaps = [.. new[] { TenantsContainer, UserTenantsContainer, OrganizationContainer }
                .Where(name => !declared.Contains(name, StringComparer.OrdinalIgnoreCase))
                .Select(name => TenantLookupLog.ContainerNotDeclared(logger, MultitenancyTypes.ConfigurationConnection, TenantPath, name))];
            return gaps.Count == 0 ? GenericResult<IHost>.Success(host) : GenericResult<IHost>.Failure(gaps);
        });
    }

    private const string TenantPath = "tenant";
    private const string TenantsContainer = "Tenants";
    private const string UserTenantsContainer = "UserTenantsConfiguration";
    private const string OrganizationContainer = "Organization";

    /// <inheritdoc/>
    public override bool EnablesTenantResolution => false;

}
