using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.ServiceTypes;
using Fdw.Services.Abstractions;
using Fdw.Services.ExternalIdentityProviders.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.ExternalIdentityProviders;

/// <summary>
/// Base class for external identity provisioner service type definitions. A 3-parameter CRTP base
/// over TService/TConfiguration/TFactory, targeting the <c>sec</c> schema — NOT <c>auth</c> — since
/// provisioners are a security-mechanism selector, not an identity-provider configuration.
/// </summary>
/// <remarks>
/// Lives in the concrete <c>Fdw.Services.ExternalIdentityProviders</c> package (net10.0), not
/// <c>Fdw.Services.ExternalIdentityProviders.Abstractions</c> (netstandard2.0). This class closes
/// <c>TProvider</c> to
/// <c>IDomainServiceProvider&lt;IExternalIdentityProvisioner, ExternalIdentityProvisionerConfiguration&gt;</c>,
/// and <c>IExternalIdentityProvisionerImplementationConfiguration</c> is only available from this package (its
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
               defaultDataStoreName: "PlatformConfiguration",
               defaultPathName: "sec",
               defaultContainerName: defaultContainerName)
    {
    }

    // ── Domain-provider/domain-configuration-provider registration ─────────────────────────────
    // An overload of Registration/Register, not a new phase and not a new method name. Stored and
    // invoked exactly like the DI-wiring Registration(Func<IHostApplicationBuilder, ...>) overload
    // already inherited from ServiceTypeBase -- this is simply a second signature of the same verb,
    // distinguished by its parameter types.

    private Func<IServiceProvider, IExternalIdentityProvisionerServiceProvider?, IExternalIdentityProvisionerConfigurationProvider?, ILogger<IExternalIdentityProvisionerType>, IGenericResult> _domainRegistrationMethod
        = static (_, _, _, _) => GenericResult.Success();

    /// <inheritdoc/>
    public void Registration(Func<IServiceProvider, IExternalIdentityProvisionerServiceProvider?, IExternalIdentityProvisionerConfigurationProvider?, ILogger<IExternalIdentityProvisionerType>, IGenericResult> method)
    {
        _domainRegistrationMethod = method;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Base default: an option that never called the overload above reports success and leaves
    /// both providers untouched.
    /// </remarks>
    public IGenericResult Register(IServiceProvider serviceProvider, IExternalIdentityProvisionerServiceProvider? domainProvider, IExternalIdentityProvisionerConfigurationProvider? domainConfigurationProvider, ILogger<IExternalIdentityProvisionerType> logger)
        => _domainRegistrationMethod(serviceProvider, domainProvider, domainConfigurationProvider, logger);
}
