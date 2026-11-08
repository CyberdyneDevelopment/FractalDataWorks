using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Operations.Abstractions.Escalation;
using Fdw.Operations.Abstractions.Execution;
using Fdw.Operations.Abstractions.Thresholds;
using Fdw.Operations.Thresholds;
using Fdw.Operations.Commands;
using Fdw.Operations.Configuration;
using Fdw.Operations.Escalation;
using Fdw.Operations.Execution;
using Fdw.Operations.Logging;
using Fdw.Services.Abstractions;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Notifications;
using Fdw.Services.Notifications.Abstractions;
using Fdw.Services.Notifications.Configuration;
using Fdw.ServiceTypes;
using Fdw.Services.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Fdw.Results;

using Fdw.Services.Results;

using Microsoft.Extensions.Logging.Abstractions;
using Fdw.Operations.Abstractions.Audit;
using Fdw.Operations.Audit;

namespace Fdw.Operations;

/// <summary>
/// Default operations service type. Registers execution tracking (IExecutionTracker),
/// escalation (IEscalationService), and the gateway-backed EscalationConfigurationProvider.
/// </summary>
[ExcludeFromCodeCoverage]
[Implementation(typeof(OperationsServiceTypes), "Default")]
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
        // The implementation provider is registered under every threshold type, since the Implementation of a threshold row is the type's name (FDW-3936). Done here, after
        // Build, so a type that cannot be registered stops the host with a failed result instead of throwing out of a singleton factory.
        Initialization((host, loggerFactory) =>
        {
            var domain = host.Services.GetRequiredService<ThresholdConfigurationProvider>();
            var implementation = host.Services.GetRequiredService<IThresholdImplementationConfigurationProvider>();
            foreach (var type in ThresholdTypes.All())
            {
                var registered = domain.Register(type.Name, implementation);
                if (registered.IsFailure)
                {
                    return registered.ToNewResult<IHost>();
                }
            }

            return GenericResult<IHost>.Success(host);
        });

        Registration((builder, loggerFactory) =>
        {
            if (OperationsServiceTypes.LatestRunsChunkSize < 1)
            {
                return GenericResult<IHostApplicationBuilder>.Failure(
                    OperationsLog.LatestRunsChunkSizeInvalid((loggerFactory ?? NullLoggerFactory.Instance).CreateLogger<DefaultOperationsServiceType>(), OperationsServiceTypes.LatestRunsChunkSize));
            }

            builder.Services.AddSingleton<IEscalationPolicyImplementationConfigurationProvider, EscalationPolicyImplementationConfigurationProvider>(sp => new EscalationPolicyImplementationConfigurationProvider(sp.GetRequiredService<ILogger<EscalationPolicyImplementationConfigurationProvider>>(), sp.GetRequiredService<IConfigurationGatewayProvider>(), OperationsServiceTypes.ConfigurationConnection, "workflow", "EscalationPolicyConfiguration"));
            builder.Services.TryAddSingleton<EscalationConfigurationProvider>(sp =>
            {
                var domain = new EscalationConfigurationProvider(
                    sp.GetRequiredService<ILogger<EscalationConfigurationProvider>>(),
                    sp.GetRequiredService<IConfigurationGatewayProvider>(), OperationsServiceTypes.ConfigurationConnection, "workflow", "EscalationPolicy");
                domain.Register("EscalationPolicy", sp.GetRequiredService<IEscalationPolicyImplementationConfigurationProvider>());
                return domain;
            });
            builder.Services.TryAddSingleton<IEscalationConfigurationProvider>(sp => sp.GetRequiredService<EscalationConfigurationProvider>());


            // The thresholds (FDW-3936).
            builder.Services.AddSingleton<IThresholdImplementationConfigurationProvider, ThresholdImplementationConfigurationProvider>(sp => new ThresholdImplementationConfigurationProvider(sp.GetRequiredService<ILogger<ThresholdImplementationConfigurationProvider>>(), sp.GetRequiredService<IConfigurationGatewayProvider>(), OperationsServiceTypes.ConfigurationConnection, "workflow", "ThresholdConfiguration"));
            builder.Services.TryAddSingleton<ThresholdConfigurationProvider>(sp => new ThresholdConfigurationProvider(
                sp.GetRequiredService<ILogger<ThresholdConfigurationProvider>>(),
                sp.GetRequiredService<IConfigurationGatewayProvider>(), OperationsServiceTypes.ConfigurationConnection, "workflow", "Threshold"));
            builder.Services.TryAddSingleton<IThresholdConfigurationProvider>(sp => sp.GetRequiredService<ThresholdConfigurationProvider>());
            builder.Services.TryAddScoped<IThresholdSource>(sp => new ThresholdSource(
                sp.GetRequiredService<IConfigurationGatewayProvider>(), sp.GetRequiredService<IThresholdConfigurationProvider>(),
                OperationsServiceTypes.ConfigurationConnection, sp.GetRequiredService<ILoggerFactory>().CreateLogger<ThresholdSource>()));
            builder.Services.TryAddScoped<IThresholdLinks>(sp => new ThresholdLinks(
                sp.GetRequiredService<IConfigurationGatewayProvider>(), sp.GetRequiredService<IThresholdConfigurationProvider>(),
                OperationsServiceTypes.ConfigurationConnection, sp.GetRequiredService<ILoggerFactory>().CreateLogger<ThresholdLinks>()));
            builder.Services.TryAddScoped<IThresholdNotifier>(sp => new ThresholdNotifier(
                sp.GetService<INotificationServiceProvider>(), sp.GetService<INotificationRuleConfigurationProvider>(), sp.GetRequiredService<ILoggerFactory>().CreateLogger<ThresholdNotifier>()));
            builder.Services.TryAddSingleton<ThresholdEntryTracker>();
            builder.Services.TryAddScoped<IThresholdEvaluator>(sp => new ThresholdEvaluator(
                sp.GetRequiredService<IThresholdSource>(), sp, sp.GetRequiredService<ThresholdEntryTracker>(), sp.GetRequiredService<ILoggerFactory>().CreateLogger<ThresholdEvaluator>()));

            builder.Services.TryAddScoped<IExecutionTracker>(sp =>
            {
                var lf = sp.GetRequiredService<ILoggerFactory>();
                var notificationProvider = sp.GetService<INotificationServiceProvider>();
                var ruleProvider = sp.GetService<INotificationRuleConfigurationProvider>();
                return new ExecutionTrackingService(
                    sp.GetRequiredService<IDataGatewayProvider>(), lf, sp.GetRequiredService<IAuthenticationContextAccessor>(),
                    OperationsServiceTypes.OperationalConnection, notificationProvider, ruleProvider, OperationsServiceTypes.LatestRunsChunkSize);
            });

            builder.Services.TryAddScoped<IAdministrativeAudit>(sp => new AdministrativeAuditService(
                sp.GetRequiredService<IDataGatewayProvider>(), sp.GetRequiredService<ILoggerFactory>(),
                sp.GetRequiredService<IAuthenticationContextAccessor>(), sp.GetRequiredService<IUserNames>(), OperationsServiceTypes.OperationalConnection));

            builder.Services.TryAddScoped<IEscalationService>(sp =>
            {
                var lf = sp.GetRequiredService<ILoggerFactory>();
                return new EscalationService(sp.GetRequiredService<IEscalationConfigurationProvider>(), lf);
            });

            // The alerts escalation policies walk (FDW-3863): scoped, since the data gateway, the policies and the notification services are.
            builder.Services.TryAddSingleton(TimeProvider.System);
            builder.Services.TryAddScoped<IEscalationAlerts>(sp => new EscalationAlerts(
                new EscalationAlertStore(sp.GetRequiredService<IDataGatewayProvider>(), OperationsServiceTypes.OperationalConnection, sp.GetRequiredService<ILoggerFactory>().CreateLogger<EscalationAlertStore>()),
                sp.GetRequiredService<IEscalationService>(), sp.GetRequiredService<INotificationServiceProvider>(),
                sp.GetRequiredService<IEffectiveSettingsProvider>(), sp.GetRequiredService<IAuthenticationContextAccessor>(),
                sp.GetRequiredService<TimeProvider>(), sp.GetRequiredService<ILoggerFactory>().CreateLogger<EscalationAlerts>()));
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });

    }

}
