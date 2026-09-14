using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.ServiceTypes;
using Fdw.Services.Identity.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Identity;

/// <summary>
/// Base class for identity service type definitions — the mechanisms by which this process can prove
/// its own identity to an external authority.
/// </summary>
/// <remarks>
/// Lives in the concrete <c>Fdw.Services.Identity</c> package (net10.0) rather than
/// <c>Fdw.Services.Identity.Abstractions</c> (netstandard2.0), for the same reason as
/// <c>TokenManagerTypeBase</c>: this class closes <c>TConfiguration</c> over
/// <c>IIdentityServiceImplementationConfiguration</c>, whose <c>[GenerateMapper]</c> /
/// <c>[ManagedConfiguration]</c> generators are net10.0-only.
/// </remarks>
/// <typeparam name="TService">The identity service type.</typeparam>
/// <typeparam name="TConfiguration">The identity configuration type.</typeparam>
/// <typeparam name="TFactory">The factory type for creating identity service instances.</typeparam>
[ExcludeFromCodeCoverage(Justification = "Abstract base class with property definitions and constructor-only logic")]
public abstract class IdentityServiceTypeBase<TService, TConfiguration, TFactory> :
    ServiceTypeBase<TService, TFactory, TConfiguration>,
    IIdentityServiceType<TService, TConfiguration, TFactory>
    where TService : IIdentityService
    where TConfiguration : class, IGenericConfiguration
    where TFactory : IIdentityServiceFactory<TService, TConfiguration>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IdentityServiceTypeBase{TService, TConfiguration, TFactory}"/> class.
    /// </summary>
    /// <param name="name">The name of this identity mechanism.</param>
    /// <param name="category">The category for this identity type (defaults to "Identity").</param>
    /// <param name="defaultContainerName">The default container name for this identity type.</param>
    protected IdentityServiceTypeBase(
        string name,
        string? category = null,
        string defaultContainerName = "")
        : base(name, $"Identities:{name}", $"{name} Identity", $"Managed identity using {name}", category ?? "Identity",
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

    private Func<IServiceProvider, IIdentityServiceProvider?, IIdentityServiceConfigurationProvider?, ILogger<IIdentityServiceType>, IGenericResult> _domainRegistrationMethod
        = static (_, _, _, _) => GenericResult.Success();

    /// <inheritdoc/>
    public void Registration(Func<IServiceProvider, IIdentityServiceProvider?, IIdentityServiceConfigurationProvider?, ILogger<IIdentityServiceType>, IGenericResult> method)
    {
        _domainRegistrationMethod = method;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Base default: an option that never called the overload above reports success and leaves
    /// both providers untouched.
    /// </remarks>
    public IGenericResult Register(IServiceProvider serviceProvider, IIdentityServiceProvider? domainProvider, IIdentityServiceConfigurationProvider? domainConfigurationProvider, ILogger<IIdentityServiceType> logger)
        => _domainRegistrationMethod(serviceProvider, domainProvider, domainConfigurationProvider, logger);
}
