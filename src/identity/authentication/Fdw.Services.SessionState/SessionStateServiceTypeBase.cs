using Fdw.Abstractions;
using Fdw.Services.Abstractions;
using Fdw.ServiceTypes;
using Fdw.Conventions;

namespace Fdw.Services.SessionState;

/// <summary>
/// Base class for session state service type definitions.
/// </summary>
/// <typeparam name="TService">The session state service type.</typeparam>
/// <typeparam name="TFactory">The factory type for creating session state service instances.</typeparam>
public abstract class SessionStateServiceTypeBase<TService, TFactory> :
    ServiceTypeBase<TService, TFactory, IServiceConfiguration>,
    ISessionStateServiceType
    where TService : IGenericService
    where TFactory : IServiceFactory<TService, IServiceConfiguration>
{
    /// <summary>The category this kind of type is listed under when a type states none of its own.</summary>
    [PlatformDefault]
    public const string DefaultCategory = "SessionState";

    /// <summary>
    /// Initializes a new instance of the <see cref="SessionStateServiceTypeBase{TService, TFactory}"/> class.
    /// </summary>
    /// <param name="name">The name of the session state service type.</param>
    /// <param name="sectionName">The configuration section name.</param>
    /// <param name="displayName">The display name.</param>
    /// <param name="description">The description.</param>
    /// <param name="category">The optional category.</param>
    protected SessionStateServiceTypeBase(
        string name,
        string sectionName,
        string displayName,
        string description,
        string? category = null)
        : base(name, sectionName, displayName, description, category ?? DefaultCategory)
    {
    }
}
