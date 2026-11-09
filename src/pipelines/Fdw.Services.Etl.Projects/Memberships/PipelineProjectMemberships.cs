using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Etl.Projects.Abstractions;
using Fdw.Services.Etl.Projects.Abstractions.Configuration;
using Fdw.Services.Etl.Projects.Abstractions.TypeCollections;
using Fdw.Services.Etl.Projects.Logging;
using Fdw.Services.Etl.Projects.Providers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Projects.Memberships;

/// <summary>
/// Reads which projects run each pipeline, from the orchestration nodes the caller may see (FDW-4449).
/// </summary>
/// <remarks>
/// <para>
/// Pipelines hang off Step nodes only, and a Step sits under a Stage under a Project. The nodes are read flat with their memberships
/// (<c>GetFlat</c> of the domain provider): the domain's rows, their implementation rows and each child collection once, a fixed number of statements however many nodes
/// there are, where hanging them into trees would re-read the domain for each node. The project of a node is then found in memory by walking its parent link to the first
/// Project node, at any depth.
/// </para>
/// <para>
/// Visibility is the nodes': a project the caller cannot see is not in the list, so its memberships are not read, and its name is never returned. The membership table has
/// no tenant column of its own.
/// </para>
/// </remarks>
public sealed class PipelineProjectMemberships : IPipelineProjectMemberships
{
    private readonly OrchestrationNodeConfigurationProvider _nodes;
    private readonly ILogger<PipelineProjectMemberships> _logger;

    /// <summary>Initializes a new instance of the <see cref="PipelineProjectMemberships"/> class.</summary>
    /// <param name="nodes">Reads the orchestration nodes.</param>
    /// <param name="logger">The logger the read reports through.</param>
    public PipelineProjectMemberships(OrchestrationNodeConfigurationProvider nodes, ILogger<PipelineProjectMemberships>? logger = null)
    {
        _nodes = nodes ?? throw new ArgumentNullException(nameof(nodes));
        _logger = logger ?? NullLogger<PipelineProjectMemberships>.Instance;
    }

    /// <inheritdoc/>
    public async Task<IGenericResult<IReadOnlyDictionary<Guid, IReadOnlyList<string>>>> ByPipeline(CancellationToken cancellationToken = default)
    {
        var read = await _nodes.GetFlat(ConfigurationLevels.Children, cancellationToken).ConfigureAwait(false);
        if (read.IsFailure)
            return read.ToNewResult<IReadOnlyDictionary<Guid, IReadOnlyList<string>>>();

        return GenericResult<IReadOnlyDictionary<Guid, IReadOnlyList<string>>>.Success(Of(read.Value!, _logger));
    }

    /// <summary>Works out, from the nodes alone, the sorted project names of each pipeline.</summary>
    /// <param name="nodes">Every node the caller may see, flat, each with its pipeline memberships.</param>
    /// <param name="logger">The logger a node with no project above it is reported to.</param>
    /// <returns>The pipeline's logical id to its sorted project names; a pipeline no project runs is absent.</returns>
    internal static Dictionary<Guid, IReadOnlyList<string>> Of(IReadOnlyList<IOrchestrationNodeImplementationConfiguration> nodes, ILogger logger)
    {
        var byId = new Dictionary<Guid, IOrchestrationNodeImplementationConfiguration>();
        foreach (var node in nodes)
            byId[node.Id] = node;

        var names = new Dictionary<Guid, SortedSet<string>>();
        var withoutProject = 0;
        foreach (var node in nodes.Where(n => n.PipelineMemberships.Count > 0))
        {
            if (ProjectOf(node, byId) is not { } project)
            {
                withoutProject++;
                continue;
            }

            foreach (var membership in node.PipelineMemberships)
            {
                if (!names.TryGetValue(membership.PipelineId, out var projects))
                {
                    projects = new SortedSet<string>(StringComparer.Ordinal);
                    names[membership.PipelineId] = projects;
                }

                projects.Add(project.Name);
            }
        }

        if (withoutProject > 0)
            PipelineProjectMembershipLog.NodesWithoutProject(logger, withoutProject);

        return names.ToDictionary(pair => pair.Key, pair => (IReadOnlyList<string>)[.. pair.Value]);
    }

    // The first Project node on the way up the parent link, or none when the link leaves the nodes the caller may see or loops back on itself.
    private static IOrchestrationNodeImplementationConfiguration? ProjectOf(
        IOrchestrationNodeImplementationConfiguration node, Dictionary<Guid, IOrchestrationNodeImplementationConfiguration> byId)
    {
        var seen = new HashSet<Guid>();
        var current = node;
        while (seen.Add(current.Id))
        {
            if (current.NodeTypeId == OrchestrationNodeTypes.Project.Id)
                return current;

            if (current.ParentId is not { } parent || !byId.TryGetValue(parent, out var next))
                return null;

            current = next;
        }

        return null;
    }
}
