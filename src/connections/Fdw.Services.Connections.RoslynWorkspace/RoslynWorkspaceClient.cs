using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Connections.RoslynWorkspace.Abstractions;
using Fdw.Services.Connections.RoslynWorkspace.Abstractions.Logging;
using Fdw.Workspace.Roslyn;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Connections.RoslynWorkspace;

/// <summary>
/// Implementation of <see cref="IRoslynWorkspaceClient"/> that wraps a resident <see cref="IRoslynWorkspace"/>.
/// This is the Live-mode client — the workspace stays loaded for the lifetime of the connection.
/// </summary>
internal sealed class RoslynWorkspaceClient : IRoslynWorkspaceClient
{
    private readonly IRoslynWorkspace _workspace;
    private readonly string _connectionName;
    private readonly ILogger _logger;

    internal RoslynWorkspaceClient(IRoslynWorkspace workspace, string connectionName, ILogger? logger)
    {
        _workspace = workspace;
        _connectionName = connectionName;
        _logger = logger ?? NullLogger.Instance;
    }

    /// <inheritdoc />
    public async Task<IGenericResult<RawText>> GetSymbolSource(
        string symbolId,
        RawTextLineRange? lines,
        CancellationToken cancellationToken = default)
    {
        RoslynWorkspaceConnectionLog.GettingSymbolSource(_logger, _connectionName, symbolId);

        try
        {
            var result = await SymbolSourceLocator.GetSymbolSource(
                _workspace.CurrentSolution,
                symbolId,
                lines,
                _connectionName,
                _logger,
                cancellationToken).ConfigureAwait(false);

            if (result.IsSuccess && result.Value is not null)
                RoslynWorkspaceConnectionLog.SymbolSourceResolved(
                    _logger, _connectionName, symbolId, result.Value.Text.Length);

            return result;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return GenericResult<RawText>.Failure(
                RoslynWorkspaceConnectionLog.WorkspaceLoadFailed(
                    _logger, ex, _connectionName, _connectionName, ex.Message));
        }
    }

    /// <inheritdoc />
    public Task<IGenericResult<RoslynSymbolMatch>> ResolveSymbol(string name, CancellationToken cancellationToken = default)
        => SymbolFinderHelper.ResolveSymbol(_workspace.CurrentSolution, name, _connectionName, _logger, cancellationToken);

    /// <inheritdoc />
    public Task<IGenericResult<IReadOnlyList<RoslynSymbolMatch>>> FindCallers(string symbolId, int max, CancellationToken cancellationToken = default)
        => SymbolFinderHelper.FindCallers(_workspace.CurrentSolution, symbolId, max, _connectionName, _logger, cancellationToken);

    /// <inheritdoc />
    public Task<IGenericResult<IReadOnlyList<RoslynSymbolMatch>>> FindCallees(string symbolId, int max, CancellationToken cancellationToken = default)
        => SymbolFinderHelper.FindCallees(_workspace.CurrentSolution, symbolId, max, _connectionName, _logger, cancellationToken);

    /// <inheritdoc />
    public Task<IGenericResult<IReadOnlyList<RoslynSymbolMatch>>> FindImplementations(string symbolId, int max, CancellationToken cancellationToken = default)
        => SymbolFinderHelper.FindImplementations(_workspace.CurrentSolution, symbolId, max, _connectionName, _logger, cancellationToken);

    /// <inheritdoc />
    public async Task<IGenericResult<WorkspaceGraph>> GetGraph(CancellationToken cancellationToken = default)
    {
        RoslynWorkspaceConnectionLog.BuildingGraph(_logger, _connectionName);

        try
        {
            var nodes = new List<WorkspaceNode>();
            var edges = new List<WorkspaceEdge>();

            foreach (var project in _workspace.CurrentSolution.Projects)
            {
                cancellationToken.ThrowIfCancellationRequested();

                nodes.Add(new WorkspaceNode(
                    project.Id.ToString(),
                    project.Name,
                    project.Language));

                foreach (var refId in project.ProjectReferences.Select(r => r.ProjectId))
                {
                    edges.Add(new WorkspaceEdge(
                        project.Id.ToString(),
                        refId.ToString(),
                        "ProjectReference"));
                }
            }

            var graph = new WorkspaceGraph(nodes, edges);
            RoslynWorkspaceConnectionLog.GraphBuilt(_logger, _connectionName, nodes.Count, edges.Count);
            return await Task.FromResult(GenericResult<WorkspaceGraph>.Success(graph)).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return GenericResult<WorkspaceGraph>.Failure(
                RoslynWorkspaceConnectionLog.WorkspaceLoadFailed(
                    _logger, ex, _connectionName, _connectionName, ex.Message));
        }
    }
}
