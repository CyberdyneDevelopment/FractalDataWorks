using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Connections;
using Fdw.Services.Connections.RoslynWorkspace.Abstractions;
using Fdw.Services.Connections.RoslynWorkspace.Abstractions.Logging;
using Fdw.Workspace.Roslyn;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Connections.RoslynWorkspace;

/// <summary>
/// RoslynWorkspace connection implementation.
/// Holds an <see cref="IRoslynWorkspace"/> internally and exposes only
/// <see cref="IRoslynWorkspaceClient"/> to consumers — per the closed-door decision in §2.
/// </summary>
/// <remarks>
/// Inherits <c>ConnectionBase&lt;IRoslynWorkspaceCommand, ...&gt;</c> so the DataGateway-routed
/// command path remains wirable in 1.2.0 if the canary experiment succeeds.
/// No concrete <see cref="IRoslynWorkspaceCommand"/> types ship in 1.1.1 — connectors call
/// <see cref="Client"/> directly per the §1.1 canary experiment.
/// </remarks>
public sealed class RoslynWorkspaceConnection
    : ConnectionBase<IRoslynWorkspaceCommand, RoslynWorkspaceConnectionConfiguration, RoslynWorkspaceConnection>,
      IRoslynWorkspaceConnection
{
    // Why: no-op translator — no DataGateway commands registered in 1.1.1.
    private static readonly IDataCommandTranslator<IRoslynWorkspaceCommand> _nullTranslator =
        new NullRoslynWorkspaceTranslator();

    private readonly IRoslynWorkspaceClient _client;
    private readonly ILogger<RoslynWorkspaceConnection> _logger;

    /// <inheritdoc />
    public string SolutionPath { get; }

    /// <inheritdoc />
    public RoslynWorkspaceModeBase Mode { get; }

    /// <inheritdoc />
    public IRoslynWorkspaceClient Client => _client;

    /// <summary>
    /// Initializes a Live-mode connection that holds the workspace resident.
    /// </summary>
    /// <param name="configuration">The typed body configuration for this Roslyn workspace connection.</param>
    /// <param name="workspace">The resident Roslyn workspace.</param>
    /// <param name="mode">The workspace mode (Live, Snapshot, etc.).</param>
    /// <param name="connectionName">The connection name from the parent ConnectionConfiguration header.</param>
    /// <param name="logger">Logger; falls back to NullLogger if null.</param>
    // Why: After config-split, Name lives on the parent ConnectionConfiguration header, not on the typed body.
    // The factory extracts it and passes it explicitly so logging and the workspace client have the correct name.
    public RoslynWorkspaceConnection(
        RoslynWorkspaceConnectionConfiguration configuration,
        IRoslynWorkspace workspace,
        RoslynWorkspaceModeBase mode,
        string connectionName,
        ILogger<RoslynWorkspaceConnection>? logger)
        : base(logger, configuration)
    {
        _logger = logger ?? NullLogger<RoslynWorkspaceConnection>.Instance;
        SolutionPath = configuration.SolutionPath;
        Mode = mode;
        _client = new RoslynWorkspaceClient(workspace, connectionName, logger);
        RoslynWorkspaceConnectionLog.Created(_logger, connectionName, mode.Name, configuration.SolutionPath);
    }

    /// <inheritdoc />
    protected override IDataCommandTranslator<IRoslynWorkspaceCommand> GetTranslator(string commandType)
    {
        // Why: No DataGateway commands ship in 1.1.1. Return the null sentinel.
        return _nullTranslator;
    }

    /// <inheritdoc />
    protected override Task<IGenericResult<T>> Execute<T>(
        IRoslynWorkspaceCommand command,
        IStorageContainer container,
        CancellationToken cancellationToken)
    {
        // Why: No native RoslynWorkspace DataGateway command path in 1.1.1.
        return Task.FromResult(
            GenericResult<T>.Failure(
                RoslynWorkspaceConnectionLog.FactoryValidationFailed(
                    _logger, Name,
                    "DataGateway commands are not supported by RoslynWorkspaceConnection in 1.1.1; use IRoslynWorkspaceClient")));
    }

    /// <inheritdoc />
    protected override Task<IGenericResult> Execute(
        IRoslynWorkspaceCommand command,
        IStorageContainer container,
        CancellationToken cancellationToken)
    {
        // Why: No native RoslynWorkspace DataGateway command path in 1.1.1.
        return Task.FromResult<IGenericResult>(
            GenericResult.Failure(
                RoslynWorkspaceConnectionLog.FactoryValidationFailed(
                    _logger, Name,
                    "DataGateway commands are not supported by RoslynWorkspaceConnection in 1.1.1; use IRoslynWorkspaceClient")));
    }

    /// <inheritdoc />
    public override Task<IGenericResult> TestConnection(CancellationToken cancellationToken = default)
    {
        // Why: The workspace was already loaded by the factory — if we got here, the connection is live.
        return Task.FromResult<IGenericResult>(GenericResult.Success());
    }

    // ── Private sentinel translator ────────────────────────────────────────────

    // Why: private nested class avoids polluting the namespace. Exists only to satisfy
    // ConnectionBase abstract requirement for 1.1.1 where no DataGateway commands ship.
    private sealed class NullRoslynWorkspaceTranslator : IDataCommandTranslator<IRoslynWorkspaceCommand>
    {
        public int Id => 0;
        object Fdw.Collections.ITypeOption.Id => 0;
        public string Name => "_Empty";
        public string Category => "RoslynWorkspace";
        public string DomainName => "RoslynWorkspace";

        public Task<IGenericResult<IRoslynWorkspaceCommand>> Translate(
            IDataCommand command,
            IStorageContainer container,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                GenericResult<IRoslynWorkspaceCommand>.Failure(
                    RoslynWorkspaceConnectionLog.FactoryValidationFailed(
                        NullLogger<RoslynWorkspaceConnection>.Instance,
                        "RoslynWorkspace",
                        "DataGateway commands are not supported in 1.1.1 — use IRoslynWorkspaceClient")));
        }
    }
}
