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
/// Snapshot-mode <see cref="IRoslynWorkspaceClient"/> that loads the workspace on first command
/// and disposes it immediately after each operation.
/// </summary>
/// <remarks>
/// Why a separate client type: Live mode keeps the workspace resident for the connection lifetime.
/// Snapshot mode loads on demand per-call, releasing ~2-4 GB of resident memory after each operation.
/// This is appropriate for batch/one-shot analysis where memory pressure is a constraint.
/// </remarks>
internal sealed class SnapshotRoslynWorkspaceClient : IRoslynWorkspaceClient
{
    private readonly IRoslynWorkspaceFactory _factory;
    private readonly string _solutionPath;
    private readonly IReadOnlyList<string> _excludePatterns;
    private readonly string _connectionName;
    private readonly ILogger _logger;

    internal SnapshotRoslynWorkspaceClient(
        IRoslynWorkspaceFactory factory,
        string solutionPath,
        IReadOnlyList<string> excludePatterns,
        string connectionName,
        ILogger? logger)
    {
        _factory = factory;
        _solutionPath = solutionPath;
        _excludePatterns = excludePatterns;
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

        IRoslynWorkspace? workspace = null;
        try
        {
            workspace = await LoadWorkspace(cancellationToken).ConfigureAwait(false);
            if (!workspace.CurrentSolution.Projects.Any())
                return GenericResult<RawText>.Failure(
                    RoslynWorkspaceConnectionLog.WorkspaceLoadFailed(
                        _logger, new InvalidOperationException("Empty workspace"),
                        _connectionName, _solutionPath, "Workspace loaded but has no projects"));

            var result = await SymbolSourceLocator.GetSymbolSource(
                workspace.CurrentSolution,
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
                    _logger, ex, _connectionName, _solutionPath, ex.Message));
        }
        finally
        {
            if (workspace is IDisposable disposable)
            {
                disposable.Dispose();
                RoslynWorkspaceConnectionLog.SnapshotWorkspaceDisposed(_logger, _connectionName);
            }
        }
    }

    /// <inheritdoc />
    public async Task<IGenericResult<WorkspaceGraph>> GetGraph(CancellationToken cancellationToken = default)
    {
        RoslynWorkspaceConnectionLog.BuildingGraph(_logger, _connectionName);

        IRoslynWorkspace? workspace = null;
        try
        {
            workspace = await LoadWorkspace(cancellationToken).ConfigureAwait(false);

            var nodes = new List<WorkspaceNode>();
            var edges = new List<WorkspaceEdge>();

            foreach (var project in workspace.CurrentSolution.Projects)
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
            return GenericResult<WorkspaceGraph>.Success(graph);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return GenericResult<WorkspaceGraph>.Failure(
                RoslynWorkspaceConnectionLog.WorkspaceLoadFailed(
                    _logger, ex, _connectionName, _solutionPath, ex.Message));
        }
        finally
        {
            if (workspace is IDisposable disposable)
            {
                disposable.Dispose();
                RoslynWorkspaceConnectionLog.SnapshotWorkspaceDisposed(_logger, _connectionName);
            }
        }
    }

    /// <inheritdoc />
    public Task<IGenericResult<RoslynSymbolMatch>> ResolveSymbol(string name, CancellationToken cancellationToken = default)
        => RunOnLoadedSolution<RoslynSymbolMatch>(
            (solution, ct) => SymbolFinderHelper.ResolveSymbol(solution, name, _connectionName, _logger, ct),
            cancellationToken);

    /// <inheritdoc />
    public Task<IGenericResult<IReadOnlyList<RoslynSymbolMatch>>> FindCallers(string symbolId, int max, CancellationToken cancellationToken = default)
        => RunOnLoadedSolution<IReadOnlyList<RoslynSymbolMatch>>(
            (solution, ct) => SymbolFinderHelper.FindCallers(solution, symbolId, max, _connectionName, _logger, ct),
            cancellationToken);

    /// <inheritdoc />
    public Task<IGenericResult<IReadOnlyList<RoslynSymbolMatch>>> FindCallees(string symbolId, int max, CancellationToken cancellationToken = default)
        => RunOnLoadedSolution<IReadOnlyList<RoslynSymbolMatch>>(
            (solution, ct) => SymbolFinderHelper.FindCallees(solution, symbolId, max, _connectionName, _logger, ct),
            cancellationToken);

    /// <inheritdoc />
    public Task<IGenericResult<IReadOnlyList<RoslynSymbolMatch>>> FindImplementations(string symbolId, int max, CancellationToken cancellationToken = default)
        => RunOnLoadedSolution<IReadOnlyList<RoslynSymbolMatch>>(
            (solution, ct) => SymbolFinderHelper.FindImplementations(solution, symbolId, max, _connectionName, _logger, ct),
            cancellationToken);

    private async Task<IGenericResult<T>> RunOnLoadedSolution<T>(
        Func<Microsoft.CodeAnalysis.Solution, CancellationToken, Task<IGenericResult<T>>> work,
        CancellationToken cancellationToken)
    {
        IRoslynWorkspace? workspace = null;
        try
        {
            workspace = await LoadWorkspace(cancellationToken).ConfigureAwait(false);
            return await work(workspace.CurrentSolution, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return GenericResult<T>.Failure(
                RoslynWorkspaceConnectionLog.WorkspaceLoadFailed(_logger, ex, _connectionName, _solutionPath, ex.Message));
        }
        finally
        {
            if (workspace is IDisposable disposable)
            {
                disposable.Dispose();
                RoslynWorkspaceConnectionLog.SnapshotWorkspaceDisposed(_logger, _connectionName);
            }
        }
    }

    private Task<IRoslynWorkspace> LoadWorkspace(CancellationToken cancellationToken)
    {
        RoslynWorkspaceConnectionLog.SnapshotWorkspaceLoaded(_logger, _connectionName);
        return _factory.CreateFromSolution(_solutionPath, _excludePatterns, cancellationToken);
    }
}
