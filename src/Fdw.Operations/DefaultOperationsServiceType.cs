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

using Fdw.Services.Results;

using Microsoft.Extensions.Logging.Abstractions;

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
        Registration((builder, loggerFactory) =>
        {
            builder.Services.AddSingleton<IEscalationPolicyImplementationConfigurationProvider, EscalationPolicyImplementationConfigurationProvider>(sp => new EscalationPolicyImplementationConfigurationProvider(sp.GetRequiredService<ILogger<EscalationPolicyImplementationConfigurationProvider>>(), sp.GetRequiredService<IConfigurationGatewayProvider>(), OperationsServiceTypes.ConfigurationConnection));
            builder.Services.TryAddSingleton<EscalationConfigurationProvider>(sp =>
            {
                var domain = new EscalationConfigurationProvider(
                    sp.GetRequiredService<ILogger<EscalationConfigurationProvider>>(),
                    sp.GetRequiredService<IConfigurationGatewayProvider>(), OperationsServiceTypes.ConfigurationConnection);
                domain.Register("EscalationPolicy", sp.GetRequiredService<IEscalationPolicyImplementationConfigurationProvider>());
                return domain;
            });
            builder.Services.TryAddSingleton<IEscalationConfigurationProvider>(sp => sp.GetRequiredService<EscalationConfigurationProvider>());


            builder.Services.TryAddScoped<IExecutionTracker>(sp =>
            {
                var lf = sp.GetRequiredService<ILoggerFactory>();
                var notificationProvider = sp.GetService<INotificationServiceProvider>();
                var ruleProvider = sp.GetService<INotificationRuleConfigurationProvider>();
                return new ExecutionTrackingService(sp.GetRequiredService<IDataGatewayProvider>(), lf, OperationsServiceTypes.OperationalConnection, notificationProvider, ruleProvider);
            });

            builder.Services.TryAddScoped<IEscalationService>(sp =>
            {
                var lf = sp.GetRequiredService<ILoggerFactory>();
                return new EscalationService(sp.GetRequiredService<IEscalationConfigurationProvider>(), lf);
            });
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });

    }

}
