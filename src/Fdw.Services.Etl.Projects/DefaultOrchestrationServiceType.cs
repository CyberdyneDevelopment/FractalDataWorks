using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Abstractions;
using Fdw.Collections;
using Fdw.Results;
using Fdw.Services.Etl.Projects.Abstractions;
using Fdw.Services.Etl.Projects.Abstractions.Configuration;
using Fdw.Services.Etl.Projects.Execution;
using Fdw.Services.Etl.Projects.Policy;
using Fdw.Services.Etl.Projects.Providers;
using Fdw.Services.Etl.Projects.Validation;
using Fdw.Services.Etl.Projects.Writers;
using Fdw.Services.Pipelines;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Fdw.Services.Etl.Projects;

/// <summary>
/// The orchestration member of the pipeline-service domain: policy, validation, the node provider
/// and writer, and the execution infrastructure that runs a project's nodes.
/// </summary>
/// <remarks>
/// These registrations were previously three public static helpers on <c>OrchestrationTypes</c>
/// with no caller inside the framework, so every host had to invoke them from its own
/// <c>Program.cs</c> — the arrangement this option removes. The bodies move here unchanged in
/// substance; what changes is that the module initializer sweeps them, so a host that references
/// this package gets the domain without naming any of it.
///
/// Why it joins <see cref="PipelineServiceTypes"/> rather than a collection of its own: there is
/// one way to orchestrate, so a dedicated collection would carry exactly one member forever and
/// buy nothing that dispatch by name is for. Orchestration composes pipelines, which is why the
/// option is declared here — this assembly can name both the pipeline collection below it and the
/// orchestration types it registers, and a collection in the pipeline package could name neither.
/// </remarks>
[ExcludeFromCodeCoverage]
[ServiceTypeOption(typeof(PipelineServiceTypes), "Orchestration")]
public sealed class DefaultOrchestrationServiceType : PipelineServiceTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultOrchestrationServiceType"/> class.
    /// </summary>
    public DefaultOrchestrationServiceType()
        : base(
            "Orchestration",
            "Pipelines:Orchestration",
            "Default Orchestration",
            "Project/Stage/Step orchestration: policy, validation, node provider and execution")
    {
        Configuration(builder =>
        {
            builder.Services.Configure<ServerPolicyDefaultsOptions>(
                builder.Configuration.GetSection(ServerPolicyDefaultsOptions.SectionName));

            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });

        Registration((builder, loggerFactory) =>
        {
            var services = builder.Services;

            // Policy implementations.
            services.TryAddSingleton<IServerPolicyDefaults, ServerPolicyDefaults>();
            services.TryAddSingleton<IPolicyElevationValidator, PolicyElevationValidator>();
            services.TryAddSingleton<IEffectivePolicyResolver, EffectivePolicyResolver>();

            // Node provider (canonical, single-table).
            OrchestrationNodeConfigurationProvider.RegisterDomainConfiguration(services);

            // Validators.
            services.TryAddTransient<IValidator<OrchestrationNodeConfiguration>>(sp =>
                new OrchestrationNodeConfigurationValidator(
                    sp.GetRequiredService<IServerPolicyDefaults>()));

            services.TryAddTransient<IValidator<ProjectConfiguration>>(sp =>
                new ProjectConfigurationValidator(
                    sp.GetRequiredService<IServerPolicyDefaults>()));

            // Writer.
            services.TryAddSingleton<OrchestrationNodeConfigurationWriter>();

            // Execution infrastructure.
            // Why Singleton for the queues: each Channel is shared between all callers — endpoints
            // and the background service both hold the same instance.
            services.TryAddSingleton<OrchestrationNodeExecutionQueue>();
            services.TryAddSingleton<ProjectExecutionQueue>();

            // Why this registers the pipeline queue rather than assuming a pipeline option is present:
            // OrchestrationNodeOrchestrator takes IPipelineExecutionQueue as a non-nullable constructor
            // parameter to dispatch leaf-node pipeline runs. The BatchCopy and Streaming options
            // register it for their own use; the helper guards against an existing registration, so
            // any order of the three callers is safe.
            Fdw.Services.Etl.EtlPipelineTypes.RegisterPipelineExecutionQueue(services);

            // Why: same gap, same fix — the orchestrator also hard-requires IExecutionCompletionSignaler
            // to await pipeline completion. It is a singleton TCS registry shared with
            // PipelineExecutionBackgroundService. A host that lacked this line left the orchestrator
            // with an unresolvable dependency, and DI validation reported only the queue failure above
            // because it stops at the first unresolvable parameter.
            services.TryAddSingleton<IExecutionCompletionSignaler, ExecutionCompletionSignaler>();
            services.TryAddScoped<IOrchestrationNodeOrchestrator, OrchestrationNodeOrchestrator>();
            services.AddHostedService<OrchestrationNodeOrchestratorBackgroundService>();

            // Execution status reader.
            services.TryAddScoped<IProjectExecutionStatusReader, ProjectExecutionStatusReader>();

            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });

        Initialization((host, loggerFactory) =>
        {
            var services = host.Services;

            // Why: eagerly resolve so a registration error surfaces at startup rather than at the
            // first request that needs one of these.
            _ = services.GetRequiredService<IServerPolicyDefaults>();
            _ = services.GetRequiredService<IPolicyElevationValidator>();
            _ = services.GetRequiredService<IEffectivePolicyResolver>();
            _ = services.GetRequiredService<IOrchestrationNodeConfigurationProvider>();
            _ = services.GetRequiredService<OrchestrationNodeConfigurationWriter>();
            _ = services.GetRequiredService<OrchestrationNodeExecutionQueue>();

            // Why a throwaway scope: IProjectExecutionStatusReader is scoped, and resolving it from
            // the root provider throws under ValidateScopes. A scope is the minimal-lifetime way to
            // fail-fast validate it, mirroring ConfigurationGatewayDataStoreProvider.Initialize.
            using var scope = services.CreateScope();
            _ = scope.ServiceProvider.GetRequiredService<IProjectExecutionStatusReader>();

            return GenericResult<IHost>.Success(host);
        });
    }
}
