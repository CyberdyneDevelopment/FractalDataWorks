using Fdw.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Data.Abstractions;
using Fdw.Services.Abstractions;
using Fdw.Services.Authorization.Commands;
using Fdw.Services.Authorization.Configuration;
using Fdw.Services.Authorization.Logging;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Fdw.Services.Authorization;

/// <summary>
/// Domain configuration provider for roles. Thin wrapper over
/// <see cref="ImplementationConfigurationProviderBase{TDomainConfiguration,TImplementationConfiguration,TCommand}"/> with permission-aggregation helpers.
/// </summary>
public class RoleConfigurationProvider : ImplementationConfigurationProviderBase<IRoleImplementationConfiguration>, IAuthorizationProvider, IRoleConfigurationProvider
{

    private readonly ILogger _logger;


    /// <summary>Initializes a new instance of the <see cref="RoleConfigurationProvider"/> class.</summary>
    public RoleConfigurationProvider(
        ILogger<RoleConfigurationProvider>? logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName,
        string pathName = "authz")
        : base(logger ?? NullLogger<RoleConfigurationProvider>.Instance,
               gatewayProvider,
               dataStoreName, pathName, "Role")
    {
        _logger = logger ?? NullLogger<RoleConfigurationProvider>.Instance;
    }








}
