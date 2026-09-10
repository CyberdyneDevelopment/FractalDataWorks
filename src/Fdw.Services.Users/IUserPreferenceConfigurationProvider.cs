using Fdw.Services.Abstractions;

namespace Fdw.Services.Users;

/// <summary>Supplies the configured UserPreferences members.</summary>
public interface IUserPreferenceConfigurationProvider
    : IDomainConfigurationProvider<IUserPreferencesImplementationConfiguration>
{
}
