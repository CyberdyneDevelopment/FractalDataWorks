using Fdw.Services.Abstractions;

namespace Fdw.Services.Authentication.Flow;

/// <summary>Supplies the AuthenticationFlow domain configuration.</summary>
public interface IAuthenticationFlowConfigurationProvider
    : IDomainConfigurationProvider<IAuthenticationFlowImplementationConfiguration>
{
}
