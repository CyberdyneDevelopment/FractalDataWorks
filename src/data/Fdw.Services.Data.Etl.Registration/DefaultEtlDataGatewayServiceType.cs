using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Results;
using Fdw.Services.Data;
using Fdw.Services.Pipelines;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Fdw.Services.Data.Etl.Registration;

/// <summary>Registers the ETL data gateway (FDW-1952) in place of the standard data gateway in a host that references this package.</summary>
[ExcludeFromCodeCoverage]
[Implementation(typeof(PipelineServiceTypes), "EtlDataGateway")]
public sealed class DefaultEtlDataGatewayServiceType : PipelineServiceTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="DefaultEtlDataGatewayServiceType"/> class.</summary>
    public DefaultEtlDataGatewayServiceType()
        : base(
            "EtlDataGateway",
            "Pipelines:EtlDataGateway",
            "ETL data gateway",
            "The ETL server's data gateway: the data gateway with the ability to read a declared container at another physical path and name")
    {
        Registration((builder, loggerFactory) =>
        {
            // Why Replace and not TryAdd: the standard registration adds its factory with TryAdd, so whichever order the two run in, the
            // ETL host ends up with this one, and a host that does not reference this package keeps the standard one.
            builder.Services.Replace(ServiceDescriptor.Singleton<IDataGatewayFactory, EtlDataGatewayFactory>());
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });
    }
}
