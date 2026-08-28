using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.ServiceTypes;
using Fdw.Services.Identity.Abstractions;

namespace Fdw.Services.Identity;

/// <summary>
/// Base class for identity service type definitions — the mechanisms by which this process can prove
/// its own identity to an external authority.
/// </summary>
/// <remarks>
/// Lives in the concrete <c>Fdw.Services.Identity</c> package (net10.0) rather than
/// <c>Fdw.Services.Identity.Abstractions</c> (netstandard2.0), for the same reason as
/// <c>TokenManagerTypeBase</c>: this class closes <c>TConfiguration</c> over
/// <see cref="IdentityServiceConfiguration"/>, whose <c>[GenerateMapper]</c> /
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
}
