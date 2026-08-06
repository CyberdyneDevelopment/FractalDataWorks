using System;
using System.Collections.Generic;
using System.Linq;
using FluentValidation;
using Fdw.Services.Etl.Projects.Abstractions;
using Fdw.Services.Etl.Projects.Abstractions.Configuration;
using Fdw.Services.Etl.Projects.Execution;
using Fdw.Services.Etl.Projects.Policy;
using Fdw.Services.Etl.Projects.Providers;
using Fdw.Services.Etl.Projects.Validation;
using Fdw.Services.Etl.Projects.Writers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Projects;

/// <summary>
/// Three-phase DI registration for ETL project orchestration services.
/// Registers the OrchestrationNode provider, writer, validators, policy implementations, and
/// the recursive OrchestrationNode orchestrator (FDW-388).
/// </summary>
public static class OrchestrationTypes
{
    /// <summary>
    /// Phase 1a: Configures IOptions bindings for OrchestrationNode configuration.
    /// Call before Build().
    /// </summary>
    // Why no longer a host-called phase: this is not a ServiceTypeCollection — it has no
    // [ServiceTypeCollection], no options, and nothing sweeps it. It is an option-owned registration
    // helper, called from the ETL pipeline options exactly like EtlPipelineTypes.RegisterPipelineExecutionQueue.
    public static IHostApplicationBuilder ConfigureOrchestrationOptions(IHostApplicationBuilder builder)
    {
        if (builder == null) throw new ArgumentNullException(nameof(builder));

        builder.Services.Configure<ServerPolicyDefaultsOptions>(
            builder.Configuration.GetSection(ServerPolicyDefaultsOptions.SectionName));

        return builder;
    }

    /// <summary>
    /// Phase 1b: Registers all orchestration node services.
    /// Call before Build().
    /// </summary>
    // Why idempotent: every pipeline option calls this, so the first wins and the rest are no-ops —
    // the same guard EtlPipelineTypes.RegisterPipelineExecutionQueue uses.
    public static IHostApplicationBuilder RegisterOrchestrationServices(
        IHostApplicationBuilder builder,
        ILoggerFactory? loggerFactory = null,
        string dataStoreName = "ConfigurationDb")
    {
        if (builder == null) throw new ArgumentNullException(nameof(builder));
        var services = builder.Services;
        if (services.Any(d => d.ServiceType == typeof(IOrchestrationNodeOrchestrator)))
            return builder;

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
        // Why Singleton for Queue: the Channel must be shared between all callers (endpoints + background service).
        services.TryAddSingleton<OrchestrationNodeExecutionQueue>();
        // Why: OrchestrationNodeOrchestrator hard-requires IPipelineExecutionQueue (constructor,
        // non-nullable) to dispatch leaf-node pipeline runs, so this registrar registers it rather
        // than assuming a pipeline option is also in the graph. The BatchCopy and Streaming options
        // register it for their own use; the helper's existing-registration guard makes all three
        // callers safe in any order.
        Fdw.Services.Etl.EtlPipelineTypes.RegisterPipelineExecutionQueue(services);
        // Why: same gap, same fix as the queue above — OrchestrationNodeOrchestrator also
        // hard-requires IExecutionCompletionSignaler (constructor, non-nullable) to await pipeline
        // completion. It is a Singleton TCS registry shared between the orchestrator and
        // PipelineExecutionBackgroundService (see ExecutionCompletionSignaler remarks). Previously
        // only Reference.Etl.Server registered it directly in its own Program.cs; hosts that register
        // OrchestrationTypes without that host-level line (e.g. Reference.Api) left the orchestrator
        // with an unresolvable dependency, masked at DI-validation time by the queue failure above
        // (the validator reports the first unresolvable constructor parameter, and pipelineQueue
        // precedes signaler). TryAddSingleton is idempotent against Reference.Etl.Server's own call.
        services.TryAddSingleton<IExecutionCompletionSignaler, ExecutionCompletionSignaler>();
        services.TryAddScoped<IOrchestrationNodeOrchestrator, OrchestrationNodeOrchestrator>();
        services.AddHostedService<OrchestrationNodeOrchestratorBackgroundService>();

        // Execution status reader.
        services.TryAddScoped<IProjectExecutionStatusReader, ProjectExecutionStatusReader>();

        return builder;
    }

    /// <summary>
    /// Phase 2: Initializes orchestration node services after the DI container is built.
    /// Performs fail-fast validation of required configuration.
    /// Call after Build().
    /// </summary>
    public static IServiceProvider InitializeOrchestration(IServiceProvider services, ILoggerFactory? loggerFactory = null)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));

        // Why: Eagerly resolve to catch any DI registration errors at startup.
        _ = services.GetRequiredService<IServerPolicyDefaults>();
        _ = services.GetRequiredService<IPolicyElevationValidator>();
        _ = services.GetRequiredService<IEffectivePolicyResolver>();
        _ = services.GetRequiredService<IOrchestrationNodeConfigurationProvider>();
        _ = services.GetRequiredService<OrchestrationNodeConfigurationWriter>();
        _ = services.GetRequiredService<OrchestrationNodeExecutionQueue>();

        // Why: IProjectExecutionStatusReader is TryAddScoped (Register() above) — resolving it directly
        // from the root `services` (app.Services) throws "Cannot resolve scoped service from root
        // provider" under Development ValidateScopes. A throwaway scope is the correct, minimal-lifetime
        // way to fail-fast validate it at startup, mirroring ConfigurationGatewayDataStoreProvider.Initialize.
        using var scope = services.CreateScope();
        _ = scope.ServiceProvider.GetRequiredService<IProjectExecutionStatusReader>();

        return services;
    }
}
