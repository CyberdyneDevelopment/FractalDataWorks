using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Etl.Projects.Abstractions;
using Fdw.Services.Etl.Projects.Abstractions.Configuration;
using Fdw.Services.Etl.Projects.Commands;
using Fdw.Services.Etl.Projects.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Fdw.Services.Etl.Projects.Providers;

/// <summary>
/// Configuration provider for OrchestrationNode — the blessed self-referencing-tree carve-out.
/// </summary>
/// <remarks>
/// Why: OrchestrationNode is a self-FK tree (node.ParentRowId → the same table). The keystone base
/// <see cref="DefaultConfigurationProvider{TConfig,TCommand}"/> ComposeChildren cannot express its
/// semantics — a depth-limited Get(id,depth) and a single load-all-then-walk-in-memory pass (instead of
/// the base's per-relationship child queries, which for a self-tree would be query-per-node and lose the
/// depth bound). So the tree overloads (Get(name,domainConfigurationId), Get(id,depth), GetRoots, GetChildren) plus
/// the BuildSubtree walker stay as a sanctioned custom layer ON TOP of the base: the base loads the flat
/// rows, and these methods walk by ParentId in memory. The plain header/CRUD reads (Get(id), Get(), Save,
/// Delete) are inherited from the base unchanged — no per-domain override.
/// </remarks>
public class OrchestrationNodeConfigurationProvider
    : DefaultConfigurationProvider<OrchestrationNodeConfiguration, OrchestrationNodeConfigurationCommand>,
      IOrchestrationNodeConfigurationProvider
{
    private readonly ILogger _logger;

    /// <summary>
    /// Registers the OrchestrationNodeConfigurationProvider with DI, targeting this domain's own
    /// default location. To override, call <c>SetConfiguration</c> on the resolved singleton.
    /// </summary>
    public static void RegisterDomainConfiguration(IServiceCollection services)
    {
        services.TryAddSingleton<OrchestrationNodeConfigurationProvider>(sp =>
            new OrchestrationNodeConfigurationProvider(
                sp.GetService<ILogger<OrchestrationNodeConfigurationProvider>>(),
                sp.GetRequiredService<Lazy<IConfigurationGateway>>()));
        services.TryAddSingleton<DefaultConfigurationProvider<OrchestrationNodeConfiguration, OrchestrationNodeConfigurationCommand>>(
            sp => sp.GetRequiredService<OrchestrationNodeConfigurationProvider>());
        services.TryAddSingleton<IOrchestrationNodeConfigurationProvider>(
            sp => sp.GetRequiredService<OrchestrationNodeConfigurationProvider>());
    }

    /// <summary>Initializes a new instance of the <see cref="OrchestrationNodeConfigurationProvider"/> class.</summary>
    public OrchestrationNodeConfigurationProvider(
        ILogger<OrchestrationNodeConfigurationProvider>? logger,
        Lazy<IConfigurationGateway> lazyGateway,
        string dataStoreName = "ConfigurationDb",
        string pathName = "pipe")
        : base(
            logger ?? NullLogger<OrchestrationNodeConfigurationProvider>.Instance,
            lazyGateway,
            dataStoreName,
            pathName)
    {
        // Why NullLogger fallback: per FDW convention, ensures the provider remains functional
        // if DI does not wire up logging.
        _logger = logger ?? NullLogger<OrchestrationNodeConfigurationProvider>.Instance;
    }

    // ── Self-FK-tree carve-out ─────────────────────────────────────────────────
    // Why: the four overloads below + BuildSubtree are the sanctioned self-referencing-tree layer.
    // The base loads the flat rows (Get() list, inherited); these walk by ParentId in memory. The base
    // ComposeChildren cannot replicate this (no depth bound; per-node queries). Plain Get(id)/Get()/Save/
    // Delete are inherited from the base — deliberately NOT overridden here.

    /// <inheritdoc/>
    public async Task<IGenericResult<OrchestrationNodeConfiguration>> Get(
        string name,
        Guid? domainConfigurationId,
        CancellationToken cancellationToken = default)
    {
        var allResult = await Get(cancellationToken).ConfigureAwait(false);
        if (!allResult.IsSuccess)
            return allResult.ToNewResult<OrchestrationNodeConfiguration>();

        var match = allResult.Value?.FirstOrDefault(n =>
            string.Equals(n.Name, name, StringComparison.OrdinalIgnoreCase) &&
            n.ParentId == domainConfigurationId);
        if (match is null)
            return GenericResult<OrchestrationNodeConfiguration>.Failure(
                OrchestrationNodeConfigurationLog.NodeNotFound(_logger, name));
        return GenericResult<OrchestrationNodeConfiguration>.Success(match);
    }

    /// <inheritdoc/>
    public async Task<IGenericResult<OrchestrationNodeConfiguration>> Get(
        Guid id,
        int depth,
        CancellationToken cancellationToken = default)
    {
        // Why: Load all nodes once and build the tree in-memory to avoid N+1 queries.
        // For large deployments, a recursive CTE query would be preferred, but the base
        // DefaultConfigurationProvider only supports flat list queries. Subtree size for
        // orchestration hierarchies is bounded and manageable in-memory.
        var allResult = await Get(cancellationToken).ConfigureAwait(false);
        if (!allResult.IsSuccess)
            return allResult.ToNewResult<OrchestrationNodeConfiguration>();

        var allNodes = allResult.Value ?? [];
        var root = allNodes.FirstOrDefault(n => n.Id == id);
        if (root is null)
            return GenericResult<OrchestrationNodeConfiguration>.Failure(
                OrchestrationNodeConfigurationLog.NodeNotFoundById(_logger, id));

        BuildSubtree(root, allNodes, depth, currentDepth: 0);
        return GenericResult<OrchestrationNodeConfiguration>.Success(root);
    }

    /// <inheritdoc/>
    public async Task<IGenericResult<IReadOnlyList<OrchestrationNodeConfiguration>>> GetRoots(
        CancellationToken cancellationToken = default)
    {
        var allResult = await Get(cancellationToken).ConfigureAwait(false);
        if (!allResult.IsSuccess)
            return allResult;

        var roots = (allResult.Value ?? [])
            .Where(n => n.ParentId is null)
            .OrderBy(n => n.Ordinal)
            .ToList();

        OrchestrationNodeConfigurationLog.NodesLoaded(_logger, roots.Count);
        return GenericResult<IReadOnlyList<OrchestrationNodeConfiguration>>.Success(roots);
    }

    /// <inheritdoc/>
    public async Task<IGenericResult<IReadOnlyList<OrchestrationNodeConfiguration>>> GetChildren(
        Guid domainConfigurationId,
        CancellationToken cancellationToken = default)
    {
        var allResult = await Get(cancellationToken).ConfigureAwait(false);
        if (!allResult.IsSuccess)
            return allResult;

        // Why: match children by the parent's DURABLE Id — RowId is DB-managed and invisible; the self-FK
        // tree walks on ParentId (logical), matching GetRoots/BuildSubtree.
        var children = (allResult.Value ?? [])
            .Where(n => n.ParentId == domainConfigurationId)
            .OrderBy(n => n.Ordinal)
            .ToList();

        return GenericResult<IReadOnlyList<OrchestrationNodeConfiguration>>.Success(children);
    }

    /// <summary>
    /// Recursively populates the Children collection of each node from the flat list, bounded by depth.
    /// </summary>
    private static void BuildSubtree(
        OrchestrationNodeConfiguration node,
        IReadOnlyList<OrchestrationNodeConfiguration> allNodes,
        int depth,
        int currentDepth)
    {
        if (currentDepth >= depth)
            return;

        var children = allNodes
            .Where(n => n.ParentId == node.Id)
            .OrderBy(n => n.Ordinal)
            .ToList();

        node.Children = children;

        foreach (var child in children)
        {
            BuildSubtree(child, allNodes, depth, currentDepth + 1);
        }
    }
}
