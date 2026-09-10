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
using Fdw.Services.Users.Logging;
using Fdw.Services.Users.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Fdw.Services.Users;

/// <summary>
/// Domain configuration provider for user preferences. Sole owner of <c>usr.UserPreferences</c> gatewayProvider access.
/// Thin wrapper over <see cref="ImplementationConfigurationProviderBase{TDomainConfiguration,TImplementationConfiguration,TCommand}"/> with a by-userId query.
/// </summary>
/// <remarks>
/// All reads and writes go through <see cref="IConfigurationGateway"/>. No <see cref="Fdw.Services.Data.Abstractions.IDataGateway"/>
/// usage — usr.UserPreferences is ConfigurationDb data accessed through the config gatewayProvider, same as usr.Users.
/// </remarks>
public class UserPreferenceConfigurationProvider
    : ImplementationConfigurationProviderBase<IUserPreferencesImplementationConfiguration>
{

    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="UserPreferenceConfigurationProvider"/> class.</summary>
    public UserPreferenceConfigurationProvider(
        ILogger<UserPreferenceConfigurationProvider>? logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName,
        string pathName = "usr")
        : base(logger ?? NullLogger<UserPreferenceConfigurationProvider>.Instance,
               gatewayProvider,
               dataStoreName, pathName, "UserPreferences")
    {
        _logger = logger ?? NullLogger<UserPreferenceConfigurationProvider>.Instance;
    }

}
