using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Workspaces;

/// <summary>Supplies the Workspace implementation's own configuration.</summary>
public sealed class WorkspaceImplementationConfigurationProvider
    : ImplementationProviderBase<WorkspaceImplementationConfiguration, IWorkspaceImplementationConfiguration>,
      IWorkspaceImplementationConfigurationProvider
{
    /// <summary>Initializes a new instance of the <see cref="WorkspaceImplementationConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    /// <param name="dataStoreName">The connection this domain's configuration rows are read from and written to.</param>
    /// <param name="pathName">The schema its rows live in -- passed by its registration.</param>
    /// <param name="containerName">The container its rows live in -- passed by its registration.</param>
    public WorkspaceImplementationConfigurationProvider(
        ILogger<WorkspaceImplementationConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName,
        string pathName,
        string containerName)
        : base(logger, gatewayProvider, dataStoreName, pathName, containerName)
    {
    }
}
