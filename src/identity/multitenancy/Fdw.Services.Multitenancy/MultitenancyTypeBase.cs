using Fdw.Abstractions;
using Fdw.Services.Abstractions;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Configuration;
using Fdw.ServiceTypes;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Multitenancy.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Workspaces;

namespace Fdw.Services.Multitenancy;

/// <summary>
/// Base class for multitenancy service type definitions (the options of <see cref="MultitenancyTypes"/>).
/// </summary>
/// <remarks>
/// The domain registers tenant/org infrastructure directly in <c>Register</c> — there
/// is no runtime-resolved service instance, so this collection has no generated provider
/// (it names no <c>ProviderType</c>) and options are looked up and invoked directly by
/// <see cref="MultitenancyTypes.ByName(string)"/> rather than through a domain provider. This mirrors
/// <c>AuthorizationTypeBase</c> — a "declared choice" domain, not a "many named instances" domain.
/// </remarks>
/// <typeparam name="TFactory">The option's own marker factory interface. Each option supplies its
/// own (e.g. <see cref="ISingleTenantMultitenancyFactory"/>) — the canonical per-option-closure
/// shape (<c>MsSqlConnectionType</c>/<c>IMsSqlConnectionFactory</c>) that gives every option a
/// distinct auto-generated Id.</typeparam>
public abstract class MultitenancyTypeBase<TFactory> :
    ServiceTypeBase<IGenericService, TFactory, IServiceConfiguration>,
    IMultitenancyType
    where TFactory : IMultitenancyFactory
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MultitenancyTypeBase{TFactory}"/> class.
    /// </summary>
    /// <param name="name">The name of this multitenancy option (matches the host's <c>Multitenancy</c> row's <c>Implementation</c>).</param>
    /// <param name="sectionName">The configuration section name for this option's own settings, if any.</param>
    /// <param name="displayName">The display name for this option.</param>
    /// <param name="description">The description of what this option provides.</param>
    protected MultitenancyTypeBase(
        string name,
        string sectionName,
        string displayName,
        string description)
        : base(name, sectionName, displayName, description, category: "Multitenancy",
               defaultDataStoreName: () => PlatformConnection.Name,
               defaultPathName: "settings",
               defaultContainerName: "Multitenancy")
    {
    }

    /// <inheritdoc/>
    public virtual bool EnablesTenantResolution => false;

    private const string WorkspacePathName = "workspace";
    private const string OrganizationPathName = "tenant";

    /// <summary>
    /// Registers the request-scoped tenant/org contexts every multitenancy option needs, regardless
    /// of whether it resolves a real tenant, and the placement of a new member in its tenant's default workspace (FDW-3692). Shared so
    /// <c>SingleTenant</c> and <c>Sql</c> (and any
    /// future option) don't duplicate these statements — only exactly ONE option's
    /// <c>Register</c> ever runs per host (see <see cref="MultitenancyTypes"/> remarks),
    /// so calling this from each option is not a double-registration.
    /// </summary>
    /// <remarks>
    /// The placement is here, not in the Sql option, because every host that creates a data set, data store, pipeline or calculation needs it
    /// whatever its multitenancy option is: the ETL host and the scheduler run <c>SingleTenant</c> and make pipelines for real tenants. It reads the
    /// tenant's default organization from the configuration connection itself, so it does not depend on the organization provider the option
    /// registers. A singleton, because the providers that hold it are.
    /// </remarks>
    protected static IServiceCollection RegisterAlwaysOnContexts(IServiceCollection services)
    {
        // Tenant context (always registered - supports single-tenant mode)
        services.AddScoped<MutableTenantContext>();
        services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<MutableTenantContext>());
        services.AddScoped<IMutableTenantContext>(sp => sp.GetRequiredService<MutableTenantContext>());

        services.AddScoped<MutableOrgContext>();
        services.AddScoped<IOrgContext>(sp => sp.GetRequiredService<MutableOrgContext>());
        services.AddScoped<IMutableOrgContext>(sp => sp.GetRequiredService<MutableOrgContext>());

        // Request-scoped tenant info for audit and context (needs HttpContextAccessor)
        services.AddHttpContextAccessor();
        services.AddScoped<IRequestTenantInfo, RequestTenantInfo>();

        services.TryAddSingleton<IConfigurationPlacement>(sp => new DefaultWorkspacePlacement(
            sp.GetRequiredService<IAuthenticationContextAccessor>(),
            sp.GetRequiredService<IWorkspaceFoundingPolicy>(),
            MultitenancyTypes.ConfigurationConnection,
            WorkspacePathName,
            OrganizationPathName,
            sp.GetService<ILogger<DefaultWorkspacePlacement>>()));

        return services;
    }
}
