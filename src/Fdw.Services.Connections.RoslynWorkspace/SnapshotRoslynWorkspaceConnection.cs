using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Connections;
using Fdw.Services.Connections.RoslynWorkspace.Abstractions;
using Fdw.Services.Connections.RoslynWorkspace.Abstractions.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Connections.RoslynWorkspace;

/// <summary>
/// Snapshot-mode RoslynWorkspace connection.
/// Uses a <see cref="SnapshotRoslynWorkspaceClient"/> that loads and disposes the workspace
/// on each operation rather than keeping it resident.
/// </summary>
public sealed class SnapshotRoslynWorkspaceConnection
    : ConnectionBase<IRoslynWorkspaceCommand, RoslynWorkspaceConnectionConfiguration, SnapshotRoslynWorkspaceConnection>,
      IRoslynWorkspaceConnection
{
    private static readonly IDataCommandTranslator<IRoslynWorkspaceCommand> _nullTranslator =
        new NullRoslynTranslator();

    private readonly IRoslynWorkspaceClient _client;
    private readonly ILogger<SnapshotRoslynWorkspaceConnection> _logger;

    /// <inheritdoc />
    public string SolutionPath { get; }

    /// <inheritdoc />
    public RoslynWorkspaceModeBase Mode { get; }

    /// <inheritdoc />
    public IRoslynWorkspaceClient Client => _client;

    /// <summary>
    /// Initializes a new instance of the <see cref="SnapshotRoslynWorkspaceConnection"/> class.
    /// </summary>
    /// <param name="configuration">The typed body configuration.</param>
    /// <param name="client">The snapshot workspace client.</param>
    /// <param name="mode">The workspace mode.</param>
    /// <param name="connectionName">The connection name from the parent ConnectionConfiguration header.</param>
    /// <param name="logger">Logger; falls back to NullLogger if null.</param>
    public SnapshotRoslynWorkspaceConnection(
        RoslynWorkspaceConnectionConfiguration configuration,
        IRoslynWorkspaceClient client,
        RoslynWorkspaceModeBase mode,
        string connectionName,
        ILogger? logger)
        : base(logger as ILogger<SnapshotRoslynWorkspaceConnection>, configuration)
    {
        _logger = logger as ILogger<SnapshotRoslynWorkspaceConnection>
            ?? NullLogger<SnapshotRoslynWorkspaceConnection>.Instance;
        _client = client;
        SolutionPath = configuration.SolutionPath;
        Mode = mode;
        RoslynWorkspaceConnectionLog.Created(_logger, connectionName, mode.Name, configuration.SolutionPath);
    }

    /// <inheritdoc />
    protected override IDataCommandTranslator<IRoslynWorkspaceCommand> GetTranslator(string commandType)
        => _nullTranslator;

    /// <inheritdoc />
    protected override Task<IGenericResult<T>> Execute<T>(
        IRoslynWorkspaceCommand command,
        IStorageContainer container,
        CancellationToken cancellationToken)
    {
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
        return Task.FromResult<IGenericResult>(
            GenericResult.Failure(
                RoslynWorkspaceConnectionLog.FactoryValidationFailed(
                    _logger, Name,
                    "DataGateway commands are not supported by RoslynWorkspaceConnection in 1.1.1; use IRoslynWorkspaceClient")));
    }

    /// <inheritdoc />
    public override Task<IGenericResult> TestConnection(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IGenericResult>(GenericResult.Success());
    }

    private sealed class NullRoslynTranslator : IDataCommandTranslator<IRoslynWorkspaceCommand>
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
                        NullLogger<SnapshotRoslynWorkspaceConnection>.Instance,
                        "RoslynWorkspace",
                        "DataGateway commands are not supported in 1.1.1 — use IRoslynWorkspaceClient")));
        }
    }
}
