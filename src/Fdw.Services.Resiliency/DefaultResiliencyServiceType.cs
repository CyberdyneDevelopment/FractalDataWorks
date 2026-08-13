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
    // Why: ResiliencyExecutor wraps stage-band orchestration nodes and is consumed directly by
    // Fdw.Services.Etl.Projects.Execution.OrchestrationNodeOrchestrator. Without these
    // registrations ETL servers crash on startup ("Unable to resolve IResiliencyExecutor" or
    // "Unable to resolve IResiliencyPolicyProvider").
    // EmptyResiliencyPolicyProvider is a baseline implementation that reports no policies; callers
    // pass null policyId to bypass it. The full dual-source provider is tracked as FDW-400.
        Registration((builder, loggerFactory) =>
        {
            builder.Services.TryAddSingleton<IResiliencyPipelineFactory, ResiliencyPipelineFactory>();
            builder.Services.TryAddSingleton<IResiliencyPolicyProvider, EmptyResiliencyPolicyProvider>();
            builder.Services.TryAddSingleton<IResiliencyExecutor, ResiliencyExecutor>();
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });

    }

}
