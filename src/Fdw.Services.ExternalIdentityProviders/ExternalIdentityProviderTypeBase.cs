using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.ServiceTypes;
using Fdw.Services.Abstractions;
using Fdw.Services.ExternalIdentityProviders.Abstractions;

namespace Fdw.Services.ExternalIdentityProviders;

/// <summary>
/// Base class for external identity provider service type definitions. Structurally copies
/// <c>TokenManagerTypeBase</c> (3-parameter CRTP over TService/TConfiguration/TFactory), stripped of
/// TokenManagers' "declared choice" framing — several <see cref="ExternalIdentityProviderTypes"/>
/// options may be simultaneously active.
/// </summary>
/// <remarks>
/// Lives in the concrete <c>Fdw.Services.ExternalIdentityProviders</c> package (net10.0), not
/// <c>Fdw.Services.ExternalIdentityProviders.Abstractions</c> (netstandard2.0) — the same placement as
/// <c>TokenManagerTypeBase</c>. This class closes <c>TProvider</c> to
/// <c>IPlatformServiceProvider&lt;IExternalIdentityProvider, ExternalIdentityProviderConfiguration&gt;</c>,
/// and <see cref="ExternalIdentityProviderConfiguration"/> is only available from this package (its
/// <c>[GenerateMapper]</c>/<c>[ManagedConfiguration]</c> source generators are net10.0-only), so the
/// base class cannot live in Abstractions without breaking the package boundary.
/// </remarks>
/// <typeparam name="TService">The external identity provider service type.</typeparam>
/// <typeparam name="TConfiguration">The external identity provider configuration type.</typeparam>
/// <typeparam name="TFactory">The factory type for creating external identity provider service instances.</typeparam>
[ExcludeFromCodeCoverage(Justification = "Abstract base class with property definitions and constructor-only logic")]
public abstract class ExternalIdentityProviderTypeBase<TService, TConfiguration, TFactory> :
    ServiceTypeBase<TService, TFactory, TConfiguration>,
    IExternalIdentityProviderType<TService, TConfiguration, TFactory>
    where TService : IExternalIdentityProvider
    where TConfiguration : class, IGenericConfiguration
    where TFactory : IExternalIdentityProviderFactory<TService, TConfiguration>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExternalIdentityProviderTypeBase{TService, TConfiguration, TFactory}"/> class.
    /// </summary>
    /// <param name="name">The name of this external identity provider type.</param>
    /// <param name="category">The category for this external identity provider type (defaults to "ExternalIdentityProvider").</param>
    /// <param name="defaultContainerName">The default container name for this type's configuration provider.</param>
    protected ExternalIdentityProviderTypeBase(
        string name,
        string? category = null,
        string defaultContainerName = "")
        : base(name, $"ExternalIdentityProviders:{name}", $"{name} External Identity Provider",
               $"External identity provider using {name}", category ?? "ExternalIdentityProvider",
               defaultDataStoreName: "PlatformConfiguration",
               defaultPathName: "auth",
               defaultContainerName: defaultContainerName)
    {
    }
}
