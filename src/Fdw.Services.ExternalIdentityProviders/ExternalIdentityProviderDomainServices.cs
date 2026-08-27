using Microsoft.Extensions.DependencyInjection;

using Fdw.Services.ExternalIdentityProviders.Abstractions;

namespace Fdw.Services.ExternalIdentityProviders;

/// <summary>
/// Domain-level registration for the external-identity-provider domain — the services that exist
/// because the DOMAIN is referenced, independent of which (if any) concrete
/// <see cref="ExternalIdentityProviderTypes"/> option is present.
/// </summary>
/// <remarks>
/// <para>
/// Why this exists (FDW-624): <see cref="ExternalIdentityProviderResolver"/> was registered in exactly
/// one place — inside the Oidc TypeOption's <c>Register</c>. The resolver's consumer
/// (<c>ConnectTokenEndpointBase</c>) takes it as a required constructor dependency, so the core token
/// endpoint could not resolve its own dependencies unless the OIDC option happened to be referenced.
/// That made external-IdP login unconditional for every consumer: removing the OIDC package broke
/// <c>/connect/token</c> outright, password grant included.
/// </para>
/// <para>
/// A shared, option-agnostic service must not be registered by one concrete option. It belongs here,
/// so that referencing the domain is sufficient to make the resolver available and options remain
/// genuinely optional.
/// </para>
/// </remarks>
public static class ExternalIdentityProviderDomainServices
{
    /// <summary>
    /// Registers the option-agnostic services of the external-identity-provider domain: the header
    /// configuration provider and the <see cref="ExternalIdentityProviderResolver"/>.
    /// </summary>
    /// <remarks>
    /// Idempotent (<c>TryAdd*</c> throughout) — safe to call from every consumer's registration
    /// cascade and from each <see cref="ExternalIdentityProviderTypes"/> option, per the FDW rule that
    /// a service registers the providers it depends on.
    /// </remarks>
    /// <param name="services">The service collection to register into.</param>
    /// <returns>The same <paramref name="services"/> instance, for chaining.</returns>
    public static IServiceCollection RegisterDomainServices(IServiceCollection services)
    {
        ExternalIdentityProviderConfigurationProvider.RegisterDomainServices(services);
        ExternalIdentityProviderResolver.RegisterDomainServices(services);
        return services;
    }
}
