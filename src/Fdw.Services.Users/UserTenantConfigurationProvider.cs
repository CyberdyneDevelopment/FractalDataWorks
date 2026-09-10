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
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using CmdBuilders = Fdw.Commands.Data.Extensions;

namespace Fdw.Services.Users;

/// <summary>
/// Domain configuration provider for user-tenant memberships. Sole owner of <c>tenant.UserTenants</c> gatewayProvider access.
/// Thin wrapper over <see cref="ImplementationConfigurationProviderBase{TDomainConfiguration,TImplementationConfiguration,TCommand}"/> with
/// tenant-membership query and mutation methods.
/// </summary>
/// <remarks>
/// All reads and writes go through <see cref="IConfigurationGateway"/>. No <see cref="Fdw.Services.Data.Abstractions.IDataGateway"/>
/// usage — tenant.UserTenants is ConfigurationDb data accessed through the config gatewayProvider, same as usr.Users.
/// </remarks>
public class UserTenantConfigurationProvider : ImplementationConfigurationProviderBase<IUserTenantImplementationConfiguration>
{

    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="UserTenantConfigurationProvider"/> class.</summary>
    public UserTenantConfigurationProvider(
        ILogger<UserTenantConfigurationProvider>? logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName,
        string pathName = "tenant")
        : base(logger ?? NullLogger<UserTenantConfigurationProvider>.Instance,
               gatewayProvider,
               dataStoreName, pathName, "UserTenants")
    {
        _logger = logger ?? NullLogger<UserTenantConfigurationProvider>.Instance;
    }





    /// <summary>
    /// Sets the specified tenant as the user's default, clearing any prior default flag.
    /// </summary>
#pragma warning disable MA0051 // Why: sequential fail-loud steps read top-to-bottom; splitting hurts the row-by-row update flow.
#pragma warning restore MA0051
}
