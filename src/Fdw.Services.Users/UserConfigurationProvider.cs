using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Users.Commands;
using Fdw.Services.Users.Configuration;
using Fdw.Services.Users.Logging;
using Fdw.Services.Users.Models;
using Fdw.Services.Users.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Fdw.Services.Users;

/// <summary>
/// Domain configuration provider for users. Sole owner of <c>usr.Users</c> gatewayProvider access.
/// Thin wrapper over <see cref="ImplementationConfigurationProviderBase{TDomainConfiguration,TImplementationConfiguration,TCommand}"/> with
/// by-id, by-username, and CRUD convenience methods.
/// </summary>
/// <remarks>
/// All reads and writes go through <see cref="IConfigurationGateway"/>. No <see cref="Fdw.Services.Data.Abstractions.IDataGateway"/>
/// usage — usr.Users is ConfigurationDb data, and the schema-built ConfigurationDb store has no ConnectionId,
/// so routing through IDataGateway produces "DataStore 'ConfigurationDb' has no ConnectionId".
/// </remarks>
public class UserConfigurationProvider : ImplementationConfigurationProviderBase<IUserImplementationConfiguration>
{

    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="UserConfigurationProvider"/> class.</summary>
    public UserConfigurationProvider(
        ILogger<UserConfigurationProvider>? logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName,
        string pathName = "usr")
        : base(logger ?? NullLogger<UserConfigurationProvider>.Instance,
               gatewayProvider,
               dataStoreName, pathName, "Users")
    {
        _logger = logger ?? NullLogger<UserConfigurationProvider>.Instance;
    }








}
