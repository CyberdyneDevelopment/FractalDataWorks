using Fdw.Services.Abstractions;
using Fdw.Services.Etl.Projects.Abstractions.Configuration;

namespace Fdw.Services.Etl.Projects.Providers;

/// <summary>Supplies the OrchestrationNode implementation's own configuration to the domain that registers it.</summary>
public interface IOrchestrationNodeImplementationConfigurationProvider
    : IImplementationConfigurationProvider<IOrchestrationNodeImplementationConfiguration>
{
}
