using Microsoft.Extensions.Logging;
using Fdw.Services.Data.Abstractions;
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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Fdw.Services.Etl.Projects.Registration;

/// <summary>
/// The orchestration member of the pipeline-service domain: policy, validation, the node provider
/// and writer, and the execution infrastructure that runs a project's nodes.
/// </summary>
/// <remarks>
/// These registrations were previously three public static helpers on <c>OrchestrationTypes</c>
/// with no caller inside the framework, so every host had to invoke them from its own
/// <c>Program.cs</c> — the arrangement this option removes. The bodies move here unchanged in
/// substance; what changes is that the module initializer collects them, so a host that references
/// this package gets the domain without naming any of it.
///
/// Why it joins <see cref="PipelineServiceTypes"/> rather than a collection of its own: there is
/// one way to orchestrate, so a dedicated collection would carry exactly one member forever and
/// buy nothing that dispatch by name is for. Orchestration composes pipelines, which is why the
/// option is declared here — this assembly can name both the pipeline collection below it and the
/// orchestration types it registers, and a collection in the pipeline package could name neither.
/// </remarks>
[ExcludeFromCodeCoverage]
[Implementation(typeof(PipelineServiceTypes), "Orchestration")]
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
        Registration((builder, loggerFactory) =>
        {
            // The read side of the nodes (the providers and the project memberships) is registered by the helper the read-side option also uses, so a host that runs both gets one of each (FDW-4449).
            OrchestrationNodeReadRegistration.Register(builder.Services);

            // The server's policy defaults: configuration in the host's server config, one ServerPolicyDefault domain row
            // naming its implementation row, read through this provider by whatever needs them (FDW-3580).
            builder.Services.TryAddSingleton<IServerPolicyDefaultImplementationConfigurationProvider>(sp => new ServerPolicyDefaultImplementationConfigurationProvider(sp.GetRequiredService<ILogger<ServerPolicyDefaultImplementationConfigurationProvider>>(), sp.GetRequiredService<IConfigurationGatewayProvider>(), ServerConnection.Name, "pipe", "ServerPolicyDefaultConfiguration"));
            builder.Services.TryAddSingleton<ServerPolicyDefaultConfigurationProvider>(sp =>
            {
                var domain = new ServerPolicyDefaultConfigurationProvider(
                    sp.GetRequiredService<ILogger<ServerPolicyDefaultConfigurationProvider>>(),
                    sp.GetRequiredService<IConfigurationGatewayProvider>(), ServerConnection.Name, "pipe", "ServerPolicyDefault");
                domain.Register("ServerPolicyDefault", sp.GetRequiredService<IServerPolicyDefaultImplementationConfigurationProvider>());
                return domain;
            });
            builder.Services.TryAddSingleton<IServerPolicyDefaultConfigurationProvider>(sp => sp.GetRequiredService<ServerPolicyDefaultConfigurationProvider>());

            var services = builder.Services;

            // Policy implementations.
            services.TryAddSingleton<IPolicyElevationValidator, PolicyElevationValidator>();
            services.TryAddSingleton<IEffectivePolicyResolver, EffectivePolicyResolver>();

            // Node provider (canonical, single-table).

            // Validators: singletons (FDW-4640). They build their rules once, keep no per-call state and read the server defaults provider, itself a singleton, on every validation;
            // the singleton writer takes one for the life of the process, so a transient registration only hid that.
            services.TryAddSingleton<IValidator<OrchestrationNodeImplementationConfiguration>>(sp =>
                new OrchestrationNodeConfigurationValidator(
                    sp.GetRequiredService<IServerPolicyDefaultConfigurationProvider>()));

            services.TryAddSingleton<IValidator<ProjectConfiguration>>(sp =>
                new ProjectConfigurationValidator(
                    sp.GetRequiredService<IServerPolicyDefaultConfigurationProvider>()));

            // Writer.
            services.TryAddSingleton<OrchestrationNodeConfigurationWriter>();

            // Execution infrastructure.
            services.TryAddSingleton<OrchestrationNodeExecutionQueue>();
            services.TryAddSingleton<ProjectExecutionQueue>();

            Fdw.Services.Etl.EtlPipelineTypes.RegisterPipelineExecutionQueue(services);

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

            _ = services.GetRequiredService<IServerPolicyDefaultConfigurationProvider>();
            _ = services.GetRequiredService<IPolicyElevationValidator>();
            _ = services.GetRequiredService<IEffectivePolicyResolver>();
            _ = services.GetRequiredService<IOrchestrationNodeConfigurationProvider>();
            _ = services.GetRequiredService<OrchestrationNodeConfigurationWriter>();
            _ = services.GetRequiredService<OrchestrationNodeExecutionQueue>();

            using var scope = services.CreateScope();
            _ = scope.ServiceProvider.GetRequiredService<IProjectExecutionStatusReader>();

            return GenericResult<IHost>.Success(host);
        });
    }
}
