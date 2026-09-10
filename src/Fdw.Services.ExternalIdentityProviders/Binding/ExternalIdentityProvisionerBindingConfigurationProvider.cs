using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.ExternalIdentityProviders.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Fdw.Services.ExternalIdentityProviders.Abstractions;

namespace Fdw.Services.ExternalIdentityProviders.Binding;

/// <summary>
/// Configuration provider for ExternalIdentityProvisionerBindingConfiguration rows in
/// sec.ExternalIdentityProvisionerBinding. Reads through IConfigurationGateway — no IConfiguration
/// binding section. Adds <see cref="ResolveProvisionerName"/>, the single selector callers (e.g.
/// <c>ResolvePrincipalStepType</c>) use to pick a named provisioner for a (tenant, external issuer)
/// pair.
/// </summary>
public class ExternalIdentityProvisionerBindingConfigurationProvider
    : ImplementationConfigurationProviderBase<IExternalIdentityProvisionerBindingImplementationConfiguration>
{

    private readonly ILogger _logger;


    /// <summary>Initializes a new instance of the <see cref="ExternalIdentityProvisionerBindingConfigurationProvider"/> class.</summary>
    public ExternalIdentityProvisionerBindingConfigurationProvider(
        ILogger<ExternalIdentityProvisionerBindingConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName,
        string pathName = "sec")
        : base(logger ?? NullLogger<ExternalIdentityProvisionerBindingConfigurationProvider>.Instance,
               gatewayProvider,
               dataStoreName, pathName, "ExternalIdentityProvisionerBinding")
    {
        _logger = logger ?? NullLogger<ExternalIdentityProvisionerBindingConfigurationProvider>.Instance;
    }

}
