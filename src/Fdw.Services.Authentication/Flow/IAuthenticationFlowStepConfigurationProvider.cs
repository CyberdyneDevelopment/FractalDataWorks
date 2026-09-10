using Fdw.Services.Abstractions;

namespace Fdw.Services.Authentication.Flow;

/// <summary>Supplies the steps a flow runs, in order.</summary>
public interface IAuthenticationFlowStepConfigurationProvider
    : IImplementationConfigurationProvider<IAuthenticationFlowStepImplementationConfiguration>
{
}
