using System;
using System.Collections.Generic;
using Fdw.Data.Abstractions;
using Fdw.Services.Configuration;
using Fdw.Services.Connections.FileSystem.Commands;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Fdw.Services.Connections.FileSystem;

/// <summary>
/// Typed configuration provider for FileSystem connections.
/// </summary>
/// <remarks>
/// Queries conn.FileSystemConnection via the configurationGateway. Get(Guid id) accepts the parent
/// Connection's logical Id and routes to <c>WHERE [ConnectionId]=@p0 AND IsCurrent=1</c>
/// via the container FK key discovered from the IDataStore tree.
/// </remarks>
public class FileSystemConnectionConfigurationProvider : DefaultConfigurationProvider<FileSystemConnectionConfiguration, FileSystemConnectionConfigurationCommand>
{
    /// <summary>Initializes a new instance of <see cref="FileSystemConnectionConfigurationProvider"/>.</summary>
    public FileSystemConnectionConfigurationProvider(
        ILogger<FileSystemConnectionConfigurationProvider> logger,
        Lazy<IConfigurationGateway> lazyGateway,
        string dataStoreName = "ConfigurationDb",
        string pathName = "conn",
        Lazy<ICacheInvalidator?>? invalidator = null)
        : base(logger ?? NullLogger<FileSystemConnectionConfigurationProvider>.Instance,
               lazyGateway,
               dataStoreName, pathName,
               invalidator)
    {
    }
}
