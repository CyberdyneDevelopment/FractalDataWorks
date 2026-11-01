using Fdw.Services.Connections.RoslynWorkspace.Abstractions;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.RoslynWorkspace;
using Fdw.Workspace.Roslyn;

namespace Fdw.Services.Connections.RoslynWorkspace.Registration;

/// <summary>
/// Builds a Roslyn workspace connection, opening the workspace first when the mode needs one.
/// </summary>
/// <remarks>
/// The connection implementation whose resolution is not a secret. Opening a solution is real I/O
/// and has to happen before a connection over that workspace exists, so it happens here — the same
/// rule every other implementation follows, with a different thing being resolved. Live mode needs
/// the workspace open; Snapshot opens lazily through its own client and is handed none.
/// </remarks>
public sealed class RoslynWorkspaceConnectionProvider
    : ImplementationServiceProviderBase<IGenericConnection, IConnectionImplementationConfiguration>,
      IRoslynWorkspaceConnectionProvider
{
    private readonly IRoslynWorkspaceConnectionFactory _factory;
    private readonly IRoslynWorkspaceFactory _workspaceFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="RoslynWorkspaceConnectionProvider"/> class.
    /// </summary>
    /// <param name="factory">Builds the connection once the workspace is in hand.</param>
    /// <param name="workspaceFactory">Opens the solution.</param>
    public RoslynWorkspaceConnectionProvider(
        IRoslynWorkspaceConnectionFactory factory,
        IRoslynWorkspaceFactory workspaceFactory)
    {
        _factory = factory;
        _workspaceFactory = workspaceFactory;
    }

    /// <inheritdoc />
    public override async Task<IGenericResult<IGenericConnection>> Create(
        IConnectionImplementationConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        if (configuration is not RoslynWorkspaceConnectionConfiguration typed)
            return _factory.Create(configuration);

        // Only Live mode needs a workspace opened up front; Snapshot builds a lazy client instead.
        // An unrecognized mode is the factory's to report, so this hands it straight through.
        if (!RoslynWorkspaceModes.ByName(typed.ModeName).Name.Equals("Live", System.StringComparison.Ordinal))
            return _factory.Create(typed, workspace: null);

        var workspace = await _workspaceFactory
            .CreateFromSolution(
                typed.SolutionPath,
                typed.ExcludePatterns?.ToList() ?? new List<string>(),
                cancellationToken)
            .ConfigureAwait(false);

        return _factory.Create(typed, workspace);
    }
}
