using Fdw.Services.Abstractions;
using Fdw.Services.TokenManagers.Abstractions;

namespace Fdw.Services.TokenManagers;

/// <summary>Supplies this implementation's own configuration to the domain that registers it.</summary>
public interface IJwtTokenManagerConfigurationProvider
    : IImplementationConfigurationProvider<ITokenManagerImplementationConfiguration>
{
}
