using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Fdw.Results;

namespace Fdw.Services.Pipelines;

/// <summary>
/// Default pipeline-service type. Registers the gateway-backed
/// <see cref="PipelineServiceConfigurationProvider"/> that the pipeline endpoints depend on.
/// </summary>
[ExcludeFromCodeCoverage]
[ServiceTypeOption(typeof(PipelineServiceTypes), "Default")]
public sealed class DefaultPipelineServiceType : PipelineServiceTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultPipelineServiceType"/> class.
    /// </summary>
    public DefaultPipelineServiceType()
        : base(
            "Default",
            "PipelineService:Default",
            "Default Pipeline Service",
            "Gateway-backed pipeline configuration provider (pipe schema)")
    {
        Registration((builder, loggerFactory) =>
        {

            PipelineServiceConfigurationProvider.RegisterDomainConfiguration(builder.Services);
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });

    }

}
