using Fdw.Services.Abstractions;
using Fdw.Services.Users.Models;

namespace Fdw.Services.Users;

/// <summary>Supplies the UserPreferences implementation's own configuration to the domain that registers it.</summary>
public interface IUserPreferencesImplementationConfigurationProvider
    : IImplementationConfigurationProvider<IUserPreferencesImplementationConfiguration>
{
}
