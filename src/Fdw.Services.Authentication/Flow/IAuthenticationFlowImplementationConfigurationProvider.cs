using Fdw.Services.Abstractions;

namespace Fdw.Services.Authentication.Flow;

/// <summary>Supplies the AuthenticationFlow implementation's own configuration.</summary>
public interface IAuthenticationFlowImplementationConfigurationProvider
    : IImplementationConfigurationProvider<IAuthenticationFlowImplementationConfiguration>
{
}
