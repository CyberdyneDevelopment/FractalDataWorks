using Fdw.Services.Abstractions;

namespace Fdw.Services.Workspaces;

/// <summary>Supplies the Workspace implementation's own configuration to the domain that registers it.</summary>
public interface IWorkspaceImplementationConfigurationProvider
    : IImplementationConfigurationProvider<IWorkspaceImplementationConfiguration>
{
}
