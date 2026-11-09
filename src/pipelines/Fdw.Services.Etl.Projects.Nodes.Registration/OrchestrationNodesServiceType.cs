using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Results;
using Fdw.Services.Etl.Projects.Abstractions;
using Fdw.Services.Etl.Projects.Providers;
using Fdw.Services.Pipelines;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Fdw.Services.Etl.Projects.Nodes.Registration;

/// <summary>
/// The read side of the orchestration nodes: the node providers and the read of which projects run each pipeline (FDW-4449).
/// </summary>
/// <remarks>
/// A host that shows orchestration, such as the API that draws the map, references this package and gets the read without the orchestrator: the run side
/// (<c>DefaultOrchestrationServiceType</c>, in <c>Fdw.Services.Etl.Projects.Registration</c>) registers the same read through the same helper and adds the queues, the
/// orchestrator and its hosted service. Referencing that package from a host that only reads would start the orchestrator in its process.
/// </remarks>
[ExcludeFromCodeCoverage]
[Implementation(typeof(PipelineServiceTypes), "OrchestrationNodes")]
public sealed class OrchestrationNodesServiceType : PipelineServiceTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OrchestrationNodesServiceType"/> class.
    /// </summary>
    public OrchestrationNodesServiceType()
        : base(
            "OrchestrationNodes",
            "Pipelines:OrchestrationNodes",
            "Orchestration Nodes",
            "Orchestration node providers and the read of which projects run each pipeline, without the orchestrator")
    {
        Registration((builder, loggerFactory) =>
        {
            OrchestrationNodeReadRegistration.Register(builder.Services);
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });

        // Why resolved here: a host that has no registration for the read stops in its Initialize phase, with the missing service named, and not at the first request.
        Initialization((host, loggerFactory) =>
        {
            var services = host.Services;
            _ = services.GetRequiredService<IOrchestrationNodeConfigurationProvider>();

            using var scope = services.CreateScope();
            _ = scope.ServiceProvider.GetRequiredService<IPipelineProjectMemberships>();

            return GenericResult<IHost>.Success(host);
        });
    }
}
