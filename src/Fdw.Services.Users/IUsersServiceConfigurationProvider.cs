using Fdw.Services.Abstractions;

namespace Fdw.Services.Users;

/// <summary>Supplies the configured UsersService members.</summary>
public interface IUsersServiceConfigurationProvider
    : IDomainConfigurationProvider<IUsersServiceImplementationConfiguration>
{
}
