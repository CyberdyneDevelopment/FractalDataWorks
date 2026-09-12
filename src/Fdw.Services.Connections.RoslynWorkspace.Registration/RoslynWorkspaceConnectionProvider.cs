using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.RoslynWorkspace;

namespace Fdw.Services.Connections.RoslynWorkspace.Registration;

/// <summary>
/// Builds a Roslyn workspace connection, opening the workspace the connection names.
/// </summary>
/// <remarks>
/// The one connection implementation whose resolution is not a secret: opening a solution is real
/// I/O, and it has to happen before a connection over that workspace exists. Same rule as everywhere
/// else -- the awaiting is the provider's, the construction is the factory's.
/// </remarks>
public sealed class RoslynWorkspaceConnectionProvider
    : ImplementationServiceProviderBase<IGenericConnection, IConnectionImplementationConfiguration>,
      IRoslynWorkspaceConnectionProvider
{
    private readonly IRoslynWorkspaceConnectionFactory _factory;

    /// <summary>
    /// Initializes a new instance of the <see cref="RoslynWorkspaceConnectionProvider"/> class.
    /// </summary>
    /// <param name="factory">Builds the connection.</param>
    public RoslynWorkspaceConnectionProvider(IRoslynWorkspaceConnectionFactory factory)
        => _factory = factory;

    /// <inheritdoc />
    public override Task<IGenericResult<IGenericConnection>> Create(
        IConnectionImplementationConfiguration configuration,
        CancellationToken cancellationToken = default)
        => Task.FromResult(_factory.Create(configuration, resolvedSecret: null));
}
