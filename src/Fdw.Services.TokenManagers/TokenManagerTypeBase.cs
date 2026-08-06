using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.ServiceTypes;
using Fdw.Services.Abstractions;
using Fdw.Services.TokenManagers.Abstractions;

namespace Fdw.Services.TokenManagers;

/// <summary>
/// Base class for token manager service type definitions. Structurally copies
/// <c>SchedulerTypeBase</c> (3-parameter CRTP over TService/TConfiguration/TFactory), stripped of
/// scheduler-specific capability metadata since <see cref="ITokenManagerType"/> is a pure marker.
/// </summary>
/// <remarks>
/// Lives in the concrete <c>Fdw.Services.TokenManagers</c> package (net10.0), not
/// <c>Fdw.Services.TokenManagers.Abstractions</c> (netstandard2.0) — the same placement as
/// <c>SchedulerTypeBase</c>. This class closes <c>TProvider</c> to
/// <c>IFdwServiceProvider&lt;ITokenManager, TokenManagerConfiguration&gt;</c>, and
/// <see cref="TokenManagerConfiguration"/> is only available from this package (its
/// <c>[GenerateMapper]</c>/<c>[ManagedConfiguration]</c> source generators are net10.0-only), so the
/// base class cannot live in Abstractions without breaking the package boundary.
/// </remarks>
/// <typeparam name="TService">The token manager service type.</typeparam>
/// <typeparam name="TConfiguration">The token manager configuration type.</typeparam>
/// <typeparam name="TFactory">The factory type for creating token manager service instances.</typeparam>
[ExcludeFromCodeCoverage(Justification = "Abstract base class with property definitions and constructor-only logic")]
public abstract class TokenManagerTypeBase<TService, TConfiguration, TFactory> :
    ServiceTypeBase<TService, TFactory, TConfiguration>,
    ITokenManagerType<TService, TConfiguration, TFactory>
    where TService : ITokenManager
    where TConfiguration : class, IGenericConfiguration
    where TFactory : ITokenManagerFactory<TService, TConfiguration>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TokenManagerTypeBase{TService, TConfiguration, TFactory}"/> class.
    /// </summary>
    /// <param name="name">The name of this token manager type.</param>
    /// <param name="category">The category for this token manager type (defaults to "TokenManager").</param>
    /// <param name="defaultContainerName">The default container name for this token manager type.</param>
    protected TokenManagerTypeBase(
        string name,
        string? category = null,
        string defaultContainerName = "")
        : base(name, $"TokenManagers:{name}", $"{name} Token Manager", $"Token manager service using {name}", category ?? "TokenManager",
               defaultDataStoreName: "ConfigurationDb",
               defaultPathName: "auth",
               defaultContainerName: defaultContainerName)
    {
    }
}
