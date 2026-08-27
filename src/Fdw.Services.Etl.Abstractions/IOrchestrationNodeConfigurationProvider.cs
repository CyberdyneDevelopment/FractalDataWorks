using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Etl.Projects.Abstractions.Configuration;

namespace Fdw.Services.Etl.Projects.Abstractions;

/// <summary>
/// Configuration provider for OrchestrationNode — the self-referencing-tree (node→parent, same table) domain.
/// The tree overloads (Get(name,domainConfigurationId)/Get(id,depth)/GetRoots/GetChildren) are the sanctioned carve-out
/// over the keystone base, which loads the flat rows; the plain Get(id)/Get()/Save/Delete are the base's.
/// All typed ergonomic providers (IProjectConfigurationProvider, IStageConfigurationProvider,
/// IStepConfigurationProvider) are thin wrappers over this interface.
/// </summary>
public interface IOrchestrationNodeConfigurationProvider
{
    /// <summary>Gets a node by name within a given parent scope.</summary>
    /// <param name="name">The node name.</param>
    /// <param name="domainConfigurationId">The parent logical Id, or null for root nodes.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task<IGenericResult<OrchestrationNodeConfiguration>> Get(string name, Guid? domainConfigurationId, CancellationToken cancellationToken = default);

    /// <summary>Gets a node by its logical identifier.</summary>
    /// <param name="id">The node logical identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task<IGenericResult<OrchestrationNodeConfiguration>> Get(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a node by its logical identifier and recursively loads children to the specified depth.
    /// </summary>
    /// <param name="id">The node logical identifier.</param>
    /// <param name="depth">Maximum recursion depth (number of child levels to inflate). 0 = no children. Use <see cref="int.MaxValue"/> for unlimited.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task<IGenericResult<OrchestrationNodeConfiguration>> Get(Guid id, int depth, CancellationToken cancellationToken = default);

    /// <summary>Gets all root nodes (nodes with no parent, CanBeRoot = true types).</summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task<IGenericResult<IReadOnlyList<OrchestrationNodeConfiguration>>> GetRoots(CancellationToken cancellationToken = default);

    /// <summary>Gets all direct children of the given parent node.</summary>
    /// <param name="domainConfigurationId">The durable Id of the parent node (RowId is DB-managed and invisible).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task<IGenericResult<IReadOnlyList<OrchestrationNodeConfiguration>>> GetChildren(Guid domainConfigurationId, CancellationToken cancellationToken = default);

    /// <summary>Gets all current, non-deleted nodes (merged ctrl + cfg).</summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task<IGenericResult<IReadOnlyList<OrchestrationNodeConfiguration>>> Get(CancellationToken cancellationToken = default);

    /// <summary>Persists a node configuration (INSERT for new, UPDATE for existing by Id).</summary>
    /// <param name="config">The node configuration to persist.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task<IGenericResult<OrchestrationNodeConfiguration>> Save(OrchestrationNodeConfiguration config, CancellationToken cancellationToken = default);

    /// <summary>Soft-deletes a node configuration by its logical identifier.</summary>
    /// <param name="id">The node logical identifier to delete.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task<IGenericResult> Delete(Guid id, CancellationToken cancellationToken = default);
}
