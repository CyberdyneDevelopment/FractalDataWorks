using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.ServiceTypes;
using Fdw.Services.Abstractions;
using Fdw.Services.ExternalIdentityProviders.Abstractions;

namespace Fdw.Services.ExternalIdentityProviders;

/// <summary>
/// Base class for external identity provisioner service type definitions. Structurally copies
/// <see cref="ExternalIdentityProviderTypeBase{TService, TConfiguration, TFactory}"/> (3-parameter CRTP
/// over TService/TConfiguration/TFactory), targeting the <c>sec</c> schema — NOT <c>auth</c> — since
/// provisioners are a security-mechanism selector, not an identity-provider configuration.
/// </summary>
/// <remarks>
/// Lives in the concrete <c>Fdw.Services.ExternalIdentityProviders</c> package (net10.0), not
/// <c>Fdw.Services.ExternalIdentityProviders.Abstractions</c> (netstandard2.0) — the same placement as
/// <c>ExternalIdentityProviderTypeBase</c>. This class closes <c>TProvider</c> to
/// <c>IFdwServiceProvider&lt;IExternalIdentityProvisioner, ExternalIdentityProvisionerConfiguration&gt;</c>,
/// and <see cref="ExternalIdentityProvisionerConfiguration"/> is only available from this package (its
/// <c>[GenerateMapper]</c>/<c>[ManagedConfiguration]</c> source generators are net10.0-only), so the
/// base class cannot live in Abstractions without breaking the package boundary.
/// </remarks>
/// <typeparam name="TService">The external identity provisioner service type.</typeparam>
/// <typeparam name="TConfiguration">The external identity provisioner configuration type.</typeparam>
/// <typeparam name="TFactory">The factory type for creating external identity provisioner service instances.</typeparam>
[ExcludeFromCodeCoverage(Justification = "Abstract base class with property definitions and constructor-only logic")]
public abstract class ExternalIdentityProvisionerTypeBase<TService, TConfiguration, TFactory> :
    ServiceTypeBase<TService, TFactory, TConfiguration>,
    IExternalIdentityProvisionerType<TService, TConfiguration, TFactory>
    where TService : IExternalIdentityProvisioner
    where TConfiguration : class, IGenericConfiguration
    where TFactory : IExternalIdentityProvisionerFactory<TService, TConfiguration>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExternalIdentityProvisionerTypeBase{TService, TConfiguration, TFactory}"/> class.
    /// </summary>
    /// <param name="name">The name of this external identity provisioner type.</param>
    /// <param name="category">The category for this external identity provisioner type (defaults to "ExternalIdentityProvisioner").</param>
    /// <param name="defaultContainerName">The default container name for this type's configuration provider.</param>
    protected ExternalIdentityProvisionerTypeBase(
        string name,
        string? category = null,
        string defaultContainerName = "")
        : base(name, $"ExternalIdentityProvisioners:{name}", $"{name} External Identity Provisioner",
               $"External identity provisioner using {name}", category ?? "ExternalIdentityProvisioner",
               defaultDataStoreName: "ConfigurationDb",
               defaultPathName: "sec",
               defaultContainerName: defaultContainerName)
    {
    }
}
