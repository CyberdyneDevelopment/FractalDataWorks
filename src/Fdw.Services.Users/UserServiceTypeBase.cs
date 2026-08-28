using Fdw.Abstractions;
using Fdw.Services.Abstractions;
using Fdw.ServiceTypes;

namespace Fdw.Services.Users;

/// <summary>
/// Base class for user service type definitions.
/// </summary>
public abstract class UserServiceTypeBase : ServiceTypeBase<IGenericService, IUserServiceFactory, IServiceConfiguration>, IUserServiceType
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UserServiceTypeBase"/> class.
    /// </summary>
    /// <param name="name">The name of the user service type.</param>
    /// <param name="sectionName">The configuration section name.</param>
    /// <param name="displayName">The display name.</param>
    /// <param name="description">The description.</param>
    /// <param name="category">The optional category.</param>
    protected UserServiceTypeBase(
        string name,
        string sectionName,
        string displayName,
        string description,
        string? category = null)
        : base(name, sectionName, displayName, description, category ?? "User",
               defaultDataStoreName: "PlatformConfiguration",
               defaultPathName: "usr")
    {
    }
}
