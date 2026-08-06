using Fdw.Abstractions;
using Fdw.Services.Abstractions;
using Fdw.ServiceTypes;

namespace Fdw.Services.Authorization;

/// <summary>
/// Base class for authorization service type definitions.
/// </summary>
/// <typeparam name="TService">The authorization service type.</typeparam>
/// <typeparam name="TFactory">The factory type for creating authorization service instances.</typeparam>
public abstract class AuthorizationTypeBase<TService, TFactory> :
    ServiceTypeBase<TService, TFactory, IServiceConfiguration>,
    IAuthorizationType
    where TService : IGenericService
    where TFactory : IServiceFactory<TService, IServiceConfiguration>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AuthorizationTypeBase{TService, TFactory}"/> class.
    /// </summary>
    /// <param name="name">The name of the authorization type.</param>
    /// <param name="sectionName">The configuration section name.</param>
    /// <param name="displayName">The display name.</param>
    /// <param name="description">The description.</param>
    /// <param name="category">The category. Defaults to "Authorization".</param>
    /// <param name="defaultContainerName">The default container name for this authorization type.</param>
    protected AuthorizationTypeBase(
        string name,
        string sectionName,
        string displayName,
        string description,
        string category = "Authorization",
        string defaultContainerName = "")
        : base(name, sectionName, displayName, description, category,
               defaultDataStoreName: "ConfigurationDb",
               defaultPathName: "authz",
               defaultContainerName: defaultContainerName)
    {
    }
}
