using Fdw.Services.Abstractions;
using Fdw.Services.Etl.Projects.Abstractions.Configuration;

namespace Fdw.Services.Etl.Projects.Providers;

/// <summary>Supplies the ServerPolicyDefault implementation rows to the domain that registers it.</summary>
public interface IServerPolicyDefaultImplementationConfigurationProvider
    : IImplementationConfigurationProvider<IServerPolicyDefaultImplementationConfiguration>
{
}
