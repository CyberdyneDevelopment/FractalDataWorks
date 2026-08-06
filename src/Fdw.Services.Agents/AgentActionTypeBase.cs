using Fdw.Abstractions;
using Fdw.Services.Abstractions;
using Fdw.ServiceTypes;

namespace Fdw.Services.Agents;

/// <summary>
/// Base class for agent action service type definitions.
/// </summary>
/// <typeparam name="TService">The agent action service type.</typeparam>
/// <typeparam name="TFactory">The factory type for creating agent action service instances.</typeparam>
public abstract class AgentActionTypeBase<TService, TFactory> :
    ServiceTypeBase<TService, TFactory, IServiceConfiguration>,
    IAgentActionType
    where TService : IGenericService
    where TFactory : IServiceFactory<TService, IServiceConfiguration>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AgentActionTypeBase{TService, TFactory}"/> class.
    /// </summary>
    /// <param name="name">The name of the agent action type.</param>
    /// <param name="sectionName">The configuration section name.</param>
    /// <param name="displayName">The display name.</param>
    /// <param name="description">The description.</param>
    /// <param name="category">The optional category.</param>
    protected AgentActionTypeBase(
        string name,
        string sectionName,
        string displayName,
        string description,
        string? category = null)
        : base(name, sectionName, displayName, description, category ?? "AgentAction")
    {
    }
}
