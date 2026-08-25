using System;
using System.Collections.Generic;
using Fdw.Services.Configuration;
using Fdw.Services.Connections.RoslynWorkspace.Commands;
using Fdw.Data.Abstractions;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Fdw.Services.Connections.RoslynWorkspace;

/// <summary>Typed configuration provider for RoslynWorkspace connections.</summary>
/// <remarks>
/// Queries conn.RoslynWorkspaceConnection via the configurationGateway. Get(Guid id) accepts the parent
/// Connection's logical Id and routes to <c>WHERE [ConnectionId]=@p0 AND IsCurrent=1</c>
/// via the container FK key discovered from the IDataStore tree.
/// </remarks>
public class RoslynWorkspaceConnectionConfigurationProvider : DefaultConfigurationProvider<RoslynWorkspaceConnectionConfiguration, RoslynWorkspaceConnectionConfigurationCommand>
{
    /// <summary>Initializes a new instance of the <see cref="RoslynWorkspaceConnectionConfigurationProvider"/> class.</summary>
    public RoslynWorkspaceConnectionConfigurationProvider(
        ILogger<RoslynWorkspaceConnectionConfigurationProvider> logger,
        Lazy<IConfigurationGateway> lazyGateway,
        string dataStoreName = "ConfigurationDb",
        string pathName = "conn")
        : base(logger ?? NullLogger<RoslynWorkspaceConnectionConfigurationProvider>.Instance,
               lazyGateway,
               dataStoreName, pathName)
    {
    }
}
