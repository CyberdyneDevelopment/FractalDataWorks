using Fdw.Abstractions;
using Fdw.Services.Abstractions;

namespace Fdw.Services.Agents;

/// <summary>
/// Factory interface for creating agent action service instances.
/// </summary>
public interface IAgentActionFactory : IServiceFactory<IGenericService, IServiceConfiguration>
{
}
