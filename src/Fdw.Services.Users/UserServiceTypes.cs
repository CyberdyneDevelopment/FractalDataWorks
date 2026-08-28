using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;

namespace Fdw.Services.Users;

/// <summary>
/// ServiceTypeCollection for user service types.
/// </summary>
[ExcludeFromCodeCoverage]
[ServiceTypeCollection(
    typeof(UserServiceTypeBase),
    typeof(IUserServiceType),
    typeof(UserServiceTypes),
    ServiceCategory = "User",
    RestrictToCurrentCompilation = true)]
public partial class UserServiceTypes : ServiceTypeCollectionBase<UserServiceTypeBase, IUserServiceType>
{
    /// <summary>
    /// The connection this domain's configuration rows are read from and written to.
    /// </summary>
    public static string ConfigurationConnection { get; set; } = "PlatformConfiguration";

}
