using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Abstractions;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.Services.Connections;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.RoslynWorkspace.Abstractions;
using Fdw.Services.Connections.RoslynWorkspace.Abstractions.Logging;
using Fdw.Services.SecretManagers.Abstractions;
using Fdw.ServiceTypes;
using Fdw.Workspace.Roslyn;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Connections.RoslynWorkspace;

/// <summary>
/// Factory for creating <see cref="RoslynWorkspaceConnection"/> instances.
/// Validates SolutionPath and resolves the operating mode before building the connection.
/// </summary>
public sealed class RoslynWorkspaceConnectionFactory : IRoslynWorkspaceConnectionFactory
{
    private readonly IRoslynWorkspaceFactory _workspaceFactory;
    private readonly ILogger<RoslynWorkspaceConnectionFactory> _logger;
    private readonly ILogger<RoslynWorkspaceConnection> _connectionLogger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RoslynWorkspaceConnectionFactory"/> class.
    /// </summary>
    public RoslynWorkspaceConnectionFactory(
        IRoslynWorkspaceFactory workspaceFactory,
        ILogger<RoslynWorkspaceConnectionFactory> logger,
        ILogger<RoslynWorkspaceConnection> connectionLogger)
    {
        _workspaceFactory = workspaceFactory;
        _logger = logger ?? NullLogger<RoslynWorkspaceConnectionFactory>.Instance;
        _connectionLogger = connectionLogger ?? NullLogger<RoslynWorkspaceConnection>.Instance;
    }

    /// <inheritdoc />
    public IGenericResult<IGenericConnection> Create(IGenericConfiguration configuration)
    {
        // Why: synchronous create cannot load the workspace (async-only). Return failure;
        // callers that need a connection should use the async overload.

        // Why: After config-split, ConnectionProvider passes a composed ConnectionConfiguration
        // header. Extract connectionName from the header; typed body is irrelevant here since we
        // always return failure (sync creation is not supported for workspace connections).
        if (configuration is ConnectionConfiguration composedHeader
            && composedHeader.Configuration is RoslynWorkspaceConnectionConfiguration)
        {
            return GenericResult<IGenericConnection>.Failure(
                RoslynWorkspaceConnectionLog.FactoryValidationFailed(
                    _logger, composedHeader.Name,
                    "RoslynWorkspaceConnection requires async creation (workspace loading is async). Use Create(config, secretManager, ct)."));
        }

        if (configuration is not RoslynWorkspaceConnectionConfiguration config)
            return GenericResult<IGenericConnection>.Failure(
                RoslynWorkspaceConnectionLog.FactoryValidationFailed(
                    _logger,
                    configuration?.GetType().Name ?? "null",
                    $"Expected RoslynWorkspaceConnectionConfiguration but got {configuration?.GetType().Name ?? "null"}"));

        return GenericResult<IGenericConnection>.Failure(
            RoslynWorkspaceConnectionLog.FactoryValidationFailed(
                _logger, config.ConnectionId.ToString(),
                "RoslynWorkspaceConnection requires async creation (workspace loading is async). Use Create(config, secretManager, ct)."));
    }

    /// <inheritdoc />
    public async Task<IGenericResult<IGenericConnection>> Create(
        IGenericConfiguration configuration,
        ISecretManager? secretManager,
        CancellationToken cancellationToken = default)
    {
        // Why: After config-split, ConnectionProvider passes a composed ConnectionConfiguration
        // header. Extract connectionName and typed body from the header.
        if (configuration is ConnectionConfiguration header
            && header.Configuration is RoslynWorkspaceConnectionConfiguration typedBody)
        {
            return await Create(typedBody, header.Name, cancellationToken).ConfigureAwait(false);
        }

        if (configuration is not RoslynWorkspaceConnectionConfiguration config)
            return GenericResult<IGenericConnection>.Failure(
                RoslynWorkspaceConnectionLog.FactoryValidationFailed(
                    _logger,
                    configuration?.GetType().Name ?? "null",
                    $"Expected RoslynWorkspaceConnectionConfiguration but got {configuration?.GetType().Name ?? "null"}"));

        return await Create(config, config.ConnectionId.ToString(), cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    // Why: a Roslyn workspace is opened from a solution path on disk — it declares no authentication
    // type, so there is never a secret to resolve. Route through the same async path as the bootstrap
    // overload.
    public Task<IGenericResult<IGenericConnection>> Create(
        IGenericConfiguration configuration,
        CancellationToken cancellationToken = default)
        => Create(configuration, secretManager: null, cancellationToken);

    /// <inheritdoc />
    public IGenericResult<IGenericConnection> Create(RoslynWorkspaceConnectionConfiguration configuration)
    {
        // Why: same as the IGenericConfiguration overload — workspace loading is async-only.
        return GenericResult<IGenericConnection>.Failure(
            RoslynWorkspaceConnectionLog.FactoryValidationFailed(
                _logger, configuration.ConnectionId.ToString(),
                "RoslynWorkspaceConnection requires async creation. Use the async overload."));
    }

    private async Task<IGenericResult<IGenericConnection>> Create(
        RoslynWorkspaceConnectionConfiguration configuration,
        string connectionName,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(configuration.SolutionPath))
            return GenericResult<IGenericConnection>.Failure(
                RoslynWorkspaceConnectionLog.FactoryValidationFailed(
                    _logger, connectionName, "SolutionPath is required but was empty or whitespace"));

        if (!File.Exists(configuration.SolutionPath))
            return GenericResult<IGenericConnection>.Failure(
                RoslynWorkspaceConnectionLog.FactoryValidationFailed(
                    _logger, connectionName,
                    $"Solution file not found: {configuration.SolutionPath}"));

        if (string.IsNullOrWhiteSpace(configuration.ModeName))
            return GenericResult<IGenericConnection>.Failure(
                RoslynWorkspaceConnectionLog.FactoryValidationFailed(
                    _logger, connectionName, "ModeName is required but was empty or whitespace"));

        var modeOption = RoslynWorkspaceModes.ByName(configuration.ModeName);
        if (modeOption == RoslynWorkspaceModes.NotFound)
            return GenericResult<IGenericConnection>.Failure(
                RoslynWorkspaceConnectionLog.FactoryValidationFailed(
                    _logger, connectionName,
                    $"ModeName '{configuration.ModeName}' is not a valid RoslynWorkspace mode. Valid values: Live, Snapshot"));

        var mode = (RoslynWorkspaceModeBase)modeOption;

        IReadOnlyList<string> excludePatterns = configuration.ExcludePatterns?.ToList()
            ?? (IReadOnlyList<string>)new List<string>();

        if (mode.Name.Equals("Live", System.StringComparison.Ordinal))
        {
            RoslynWorkspaceConnectionLog.LoadingSolution(_logger, connectionName, configuration.SolutionPath);
            try
            {
                var workspace = await _workspaceFactory.CreateFromSolution(
                    configuration.SolutionPath,
                    excludePatterns,
                    cancellationToken).ConfigureAwait(false);

                return GenericResult<IGenericConnection>.Success(
                    new RoslynWorkspaceConnection(configuration, workspace, mode, connectionName, _connectionLogger));
            }
            catch (System.OperationCanceledException)
            {
                throw;
            }
            catch (System.Exception ex)
            {
                return GenericResult<IGenericConnection>.Failure(
                    RoslynWorkspaceConnectionLog.WorkspaceLoadFailed(
                        _logger, ex, connectionName, configuration.SolutionPath, ex.Message));
            }
        }

        // Snapshot mode: return a connection with a lazy-loading client; no workspace yet.
        var snapshotClient = new SnapshotRoslynWorkspaceClient(
            _workspaceFactory,
            configuration.SolutionPath,
            excludePatterns,
            connectionName,
            _connectionLogger);

        return GenericResult<IGenericConnection>.Success(
            new SnapshotRoslynWorkspaceConnection(configuration, snapshotClient, mode, connectionName, _connectionLogger));
    }

    #region IServiceFactory Implementation

    IGenericResult<T> IServiceFactory.Create<T>(IGenericConfiguration configuration)
    {
        var result = Create(configuration);
        if (!result.IsSuccess || result.Value == null)
            return result.ToNewResult<T>();

        if (result.Value is T typedResult)
            return GenericResult<T>.Success(typedResult);

        // Why: configuration.Name is explicit interface (returns string.Empty) on typed body.
        // Use GetType().Name as identifier for this error message — it's the type context we care about.
        return GenericResult<T>.Failure(
            RoslynWorkspaceConnectionLog.FactoryValidationFailed(
                _logger, configuration?.GetType().Name ?? "null",
                $"Connection is not assignable to {typeof(T).Name}"));
    }

    IGenericResult<IGenericService> IServiceFactory.Create(IGenericConfiguration configuration)
    {
        var result = Create(configuration);
        if (!result.IsSuccess || result.Value == null)
            return result.ToNewResult<IGenericService>();

        return GenericResult<IGenericService>.Success(result.Value);
    }

    #endregion
}
