using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Operations.Abstractions.Escalation;
using Fdw.Operations.Abstractions.Execution;
using Fdw.Operations.Commands;
using Fdw.Operations.Configuration;
using Fdw.Operations.Escalation;
using Fdw.Operations.Execution;
using Fdw.Services.Abstractions;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Notifications;
using Fdw.Services.Notifications.Abstractions;
using Fdw.Services.Notifications.Configuration;
using Fdw.ServiceTypes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Fdw.Results;

namespace Fdw.Operations;

/// <summary>
/// Default operations service type. Registers execution tracking (IExecutionTracker),
/// escalation (IEscalationService), and the gateway-backed EscalationConfigurationProvider.
/// </summary>
[ExcludeFromCodeCoverage]
[ServiceTypeOption(typeof(OperationsTypes), "Default")]
public sealed class DefaultOperationsServiceType : OperationsServiceTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultOperationsServiceType"/> class.
    /// </summary>
    public DefaultOperationsServiceType()
        : base(
            "Default",
            "Operations:Default",
            "Default Operations Services",
            "Execution tracking, escalation, and escalation-policy configuration")
    {
        // Why: IExecutionTracker + IEscalationService persist execution/runtime data to OpsDb (ops
        // schema), so they take the "OpsDb" data store the collection passes in. Scoped because they
        // depend on the scoped IDataGateway.
        Registration((builder, loggerFactory) =>
        {
            // Why here and not a Configure phase: escalation-policy config lives in
            // ConfigurationDb.workflow and is read through IConfigurationGateway — nothing binds from
            // IConfiguration, so this is pure DI registration with no Phase-1a concern.
            builder.Services.TryAddSingleton<EscalationConfigurationProvider>(sp =>
                new EscalationConfigurationProvider(
                    sp.GetService<ILogger<EscalationConfigurationProvider>>()!,
                    sp.GetRequiredService<Lazy<IConfigurationGateway>>()));
            builder.Services.TryAddSingleton<ImplementationConfigurationProviderBase<EscalationPolicyConfiguration, EscalationPolicyConfigurationCommand>>(
                sp => sp.GetRequiredService<EscalationConfigurationProvider>());
            builder.Services.TryAddSingleton<IServiceConfigurationProvider<EscalationPolicyConfiguration>>(
                sp => sp.GetRequiredService<EscalationConfigurationProvider>());

            builder.Services.TryAddScoped<IExecutionTracker>(sp =>
            {
                var lf = sp.GetRequiredService<ILoggerFactory>();
                var gateway = sp.GetRequiredService<IDataGateway>();
                // Why: GetService (not GetRequired) — notification providers are optional; callers
                // that don't wire notifications (e.g. reference-etl, reference-scheduler) still boot.
                var notificationProvider = sp.GetService<INotificationServiceProvider>();
                var ruleProvider = sp.GetService<IServiceConfigurationProvider<NotificationRuleConfiguration>>();
                return new ExecutionTrackingService(gateway, lf, "OpsDb", notificationProvider, ruleProvider);
            });

            builder.Services.TryAddScoped<IEscalationService>(sp =>
            {
                var lf = sp.GetRequiredService<ILoggerFactory>();
                // Why: EscalationService now delegates to EscalationConfigurationProvider (registered above
                // against ConfigurationDb.workflow — where the escalation tables live). This also corrects the
                // prior "OpsDb" store mismatch: the provider already targets ConfigurationDb.workflow.
                var provider = sp.GetRequiredService<EscalationConfigurationProvider>();
                return new EscalationService(provider, lf);
            });
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });

    }

}
