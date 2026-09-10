using Fdw.Configuration;

namespace Fdw.Services.Users.Models;

/// <summary>
/// The contract every UserPreferences implementation carries.
/// </summary>
/// <remarks>
/// The marker is what keeps the domain closed: only a configuration carrying it can be registered
/// against this domain or handed back by a read of it.
/// </remarks>
public interface IUserPreferencesImplementationConfiguration : IImplementationConfiguration
{
}
