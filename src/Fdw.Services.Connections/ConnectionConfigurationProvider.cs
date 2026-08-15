using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Configuration;
using Fdw.Services.Abstractions;
using Fdw.Services.Abstractions.Health;
using Fdw.Services.Configuration;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.Commands;
using Fdw.Services.Connections.Logging;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Fdw.Services.Connections;

/// <summary>
/// Domain-specific configuration provider for connections.
/// The polymorphic typed-body read (dispatch on <c>ServiceOptionType</c> to load the typed body row,
/// e.g. <c>conn.MsSqlConnection</c>, and attach it to <see cref="ConnectionConfiguration.Configuration"/>)
/// is composed uniformly by <see cref="DefaultConfigurationProvider{TConfig,TCommand}"/>; typed providers
/// are registered via the inherited <c>Register</c>.
/// </summary>
public class ConnectionConfigurationProvider : DefaultConfigurationProvider<ConnectionConfiguration, ConnectionConfigurationCommand>
{

    /// <summary>Initializes a new instance of the <see cref="ConnectionConfigurationProvider"/> class.</summary>
    public ConnectionConfigurationProvider(
        ILogger<ConnectionConfigurationProvider> logger,
        Lazy<IConfigurationGateway> lazyGateway,
        string dataStoreName = "ConfigurationDb",
        string pathName = "conn",
        Lazy<ICacheInvalidator?>? invalidator = null)
        : base(logger ?? NullLogger<ConnectionConfigurationProvider>.Instance,
               lazyGateway,
               dataStoreName, pathName,
               invalidator)
    {
    }
}
