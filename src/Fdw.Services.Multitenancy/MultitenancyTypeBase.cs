using Fdw.Abstractions;
using Fdw.Services.Abstractions;
using Fdw.ServiceTypes;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Multitenancy.Services;
using Microsoft.Extensions.DependencyInjection;

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
    /// <param name="name">The name of this multitenancy option (matches the host's <c>Multitenancy</c> row's <c>ServiceOptionType</c>).</param>
    /// <param name="sectionName">The configuration section name for this option's own settings, if any.</param>
    /// <param name="displayName">The display name for this option.</param>
    /// <param name="description">The description of what this option provides.</param>
    protected MultitenancyTypeBase(
        string name,
        string sectionName,
        string displayName,
        string description)
        : base(name, sectionName, displayName, description, category: "Multitenancy",
               defaultDataStoreName: "ConfigurationDb",
               defaultPathName: "settings",
               defaultContainerName: "Multitenancy")
    {
    }

    /// <inheritdoc/>
    public virtual bool EnablesTenantResolution => false;

    /// <summary>
    /// Registers the request-scoped tenant/org contexts every multitenancy option needs, regardless
    /// of whether it resolves a real tenant. Shared so <c>SingleTenant</c> and <c>Sql</c> (and any
    /// future option) don't duplicate these statements — only exactly ONE option's
    /// <c>Register</c> ever runs per host (see <see cref="MultitenancyTypes"/> remarks),
    /// so calling this from each option is not a double-registration.
    /// </summary>
    protected static IServiceCollection RegisterAlwaysOnContexts(IServiceCollection services)
    {
        // Tenant context (always registered - supports single-tenant mode)
        services.AddScoped<MutableTenantContext>();
        services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<MutableTenantContext>());
        services.AddScoped<IMutableTenantContext>(sp => sp.GetRequiredService<MutableTenantContext>());

        // Why: Org context is always registered alongside tenant context so IOrgContext is
        // injectable in every service that uses multitenancy. When OrgResolutionMiddleware
        // does not run (tenant resolution disabled), HasOrg remains false and the org tier in
        // DefaultAuthorizationService contributes zero grants.
        services.AddScoped<MutableOrgContext>();
        services.AddScoped<IOrgContext>(sp => sp.GetRequiredService<MutableOrgContext>());
        services.AddScoped<IMutableOrgContext>(sp => sp.GetRequiredService<MutableOrgContext>());

        // Request-scoped tenant info for audit and context (needs HttpContextAccessor)
        services.AddHttpContextAccessor();
        services.AddScoped<IRequestTenantInfo, RequestTenantInfo>();

        return services;
    }
}
