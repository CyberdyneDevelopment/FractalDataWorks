using Fdw.Services.Abstractions;

namespace Fdw.Services.Connections.RoslynWorkspace;

/// <summary>Supplies the Roslyn workspace connection's own configuration.</summary>
public interface IRoslynWorkspaceConnectionConfigurationProvider
    : IImplementationConfigurationProvider<RoslynWorkspaceConnectionConfiguration>
{
}
