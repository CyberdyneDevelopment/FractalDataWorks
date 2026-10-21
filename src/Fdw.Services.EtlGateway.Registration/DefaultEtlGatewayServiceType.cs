using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Results;
using Fdw.Services.Etl.Scripts;
using Fdw.Services.Pipelines;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Fdw.Services.EtlGateway.Registration;

/// <summary>Registers the ETL gateway (FDW-1479) into the pipeline service types of a host that references this package.</summary>
[ExcludeFromCodeCoverage]
[Implementation(typeof(PipelineServiceTypes), "EtlGateway")]
public sealed class DefaultEtlGatewayServiceType : PipelineServiceTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="DefaultEtlGatewayServiceType"/> class.</summary>
    public DefaultEtlGatewayServiceType()
        : base(
            "EtlGateway",
            "Pipelines:EtlGateway",
            "ETL gateway",
            "The ETL host's own path to a data store's connection, for running pipelines and scripts without the data gateway")
    {
        Registration((builder, loggerFactory) =>
        {
            // Scoped, as the data gateway is: it reads the caller's roles from the scope's authentication context.
            builder.Services.TryAddScoped<IEtlGateway, EtlGatewayService>();
            // The script service needs the gateway, so a host that has the gateway has it, and no other host is asked for it.
            builder.Services.TryAddScoped<IScriptService, ScriptService>();
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });
    }
}
