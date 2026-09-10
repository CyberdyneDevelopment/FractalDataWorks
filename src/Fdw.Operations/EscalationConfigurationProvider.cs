using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Operations.Commands;
using Fdw.Operations.Configuration;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Fdw.Operations;

/// <summary>Configuration provider for escalation policy configurations.</summary>
public class EscalationConfigurationProvider : ImplementationConfigurationProviderBase<IEscalationPolicyImplementationConfiguration>
{

    /// <summary>Initializes a new instance of the <see cref="EscalationConfigurationProvider"/> class.</summary>
    public EscalationConfigurationProvider(
        ILogger<EscalationConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName,
        string pathName = "workflow")
        : base(logger ?? NullLogger<EscalationConfigurationProvider>.Instance,
               gatewayProvider,
               dataStoreName, pathName, "EscalationPolicy")
    {
    }







}
