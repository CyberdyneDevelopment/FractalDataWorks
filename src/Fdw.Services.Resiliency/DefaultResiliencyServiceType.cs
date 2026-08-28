using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Services.Resiliency.Abstractions;
using Fdw.Services.Resiliency.Factories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Fdw.Results;

namespace Fdw.Services.Resiliency;

/// <summary>
/// Default resiliency service type that registers <see cref="IResiliencyPipelineFactory"/>
/// with the dependency injection container.
/// </summary>
[ExcludeFromCodeCoverage]
[ServiceTypeOption(typeof(ResiliencyServiceTypes), "Default")]
public sealed class DefaultResiliencyServiceType : ResiliencyServiceTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultResiliencyServiceType"/> class.
    /// </summary>
    public DefaultResiliencyServiceType()
        : base(
            "Default",
            "Resiliency:Default",
            "Default Resiliency",
            "Default resiliency services with Polly pipeline factory")
    {
        Registration((builder, loggerFactory) =>
        {
            builder.Services.TryAddSingleton<IResiliencyPipelineFactory, ResiliencyPipelineFactory>();
            builder.Services.TryAddSingleton<IResiliencyPolicyProvider, EmptyResiliencyPolicyProvider>();
            builder.Services.TryAddSingleton<IResiliencyExecutor, ResiliencyExecutor>();
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });

    }

}
