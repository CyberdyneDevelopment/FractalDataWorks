using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;

namespace Fdw.Services.Users;

/// <summary>
/// ServiceTypeCollection for user service types.
/// </summary>
// Why: RestrictToCurrentCompilation = true — the Users domain owns its own service options only.
// Credential storage is no longer grafted on as a cross-assembly UserServiceTypes option; it is a
// first-class CredentialServiceTypes domain, and Users consumes it via ICredentialServiceProvider
// resolved by UsersServiceOptions.CredentialServiceName (the connections→secret-managers pattern).
[ExcludeFromCodeCoverage]
[ServiceTypeCollection(
    typeof(UserServiceTypeBase),
    typeof(IUserServiceType),
    typeof(UserServiceTypes),
    ServiceCategory = "User",
    RestrictToCurrentCompilation = true)]
public partial class UserServiceTypes : ServiceTypeCollectionBase<UserServiceTypeBase, IUserServiceType>
{
}
