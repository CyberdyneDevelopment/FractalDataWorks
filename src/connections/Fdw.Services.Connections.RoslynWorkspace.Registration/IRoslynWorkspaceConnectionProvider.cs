using Fdw.Services.Abstractions;
using Fdw.Services.Connections.Abstractions;

namespace Fdw.Services.Connections.RoslynWorkspace.Registration;

/// <summary>
/// The RoslynWorkspace implementation of the Connection domain.
/// </summary>
public interface IRoslynWorkspaceConnectionProvider
    : IImplementationServiceProvider<IGenericConnection, IConnectionImplementationConfiguration>
{
}
