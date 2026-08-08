using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using Fdw.Abstractions;
using Fdw.Collections;
using Fdw.Services.Agents.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Fdw.Results;

namespace Fdw.Services.Agents;

/// <summary>
/// Default agent action service type that registers <see cref="IAgentActionService"/>
/// with the dependency injection container.
/// </summary>
[ExcludeFromCodeCoverage]
[ServiceTypeOption(typeof(AgentActionTypes), "Default")]
public sealed class DefaultAgentActionServiceType : AgentActionTypeBase<IGenericService, IAgentActionFactory>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultAgentActionServiceType"/> class.
    /// </summary>
    public DefaultAgentActionServiceType()
        : base(
            "Default",
            "AgentActions:Default",
            "Default Agent Actions",
            "Default agent action review service using DataGateway persistence")
    {
        Registration((builder, loggerFactory, dataStoreName, pathName, containerName) =>
        {

            // Why Scoped: AgentActionService requires IDataGateway (scoped) via constructor injection.
            // Consumed directly by per-request endpoints — no parent provider indirection to preserve.
            builder.Services.AddScoped<IAgentActionService, AgentActionService>();
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });

    }

}
