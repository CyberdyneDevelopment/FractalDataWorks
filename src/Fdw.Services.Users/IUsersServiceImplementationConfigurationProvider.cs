using Fdw.Services.Abstractions;
using Fdw.Services.Users.Configuration;

namespace Fdw.Services.Users;

/// <summary>Supplies the UsersService implementation's own configuration to the domain that registers it.</summary>
public interface IUsersServiceImplementationConfigurationProvider
    : IImplementationConfigurationProvider<IUsersServiceImplementationConfiguration>
{
}
