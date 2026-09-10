using Fdw.Services.Users.Configuration;
using Fdw.Services.Abstractions;

namespace Fdw.Services.Users;

/// <summary>Supplies the configured Users members.</summary>
public interface IUserConfigurationProvider
    : IDomainConfigurationProvider<IUserImplementationConfiguration>
{
}
