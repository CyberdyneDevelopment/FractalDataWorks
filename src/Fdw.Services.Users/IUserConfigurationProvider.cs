using Fdw.Services.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Users.Configuration;

namespace Fdw.Services.Users;

/// <summary>Supplies the configured Users members, and their list from the domain table alone.</summary>
public interface IUserConfigurationProvider
    : IDomainConfigurationProvider<IUserImplementationConfiguration>,
      IDomainRecordProvider<UserDomainConfiguration>
{
}
