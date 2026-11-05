using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Abstractions;
using Fdw.Collections;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Pipelines.Abstractions;
using Fdw.Services.Pipelines.Commands;
using Fdw.Services.Pipelines.Scripts;
using Fdw.Services.Pipelines.Webhooks;
using Fdw.ServiceTypes;
using Fdw.ServiceTypes.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Fdw.Services.Results;

namespace Fdw.Services.Pipelines;

/// <summary>
/// ServiceTypeCollection for the pipeline-service domain (gateway-backed pipeline
/// configuration provider). Distinct from the EtlPipelineTypes engine collection.
///
/// Why discovery is not restricted to this compilation: the orchestration domain
/// (Fdw.Services.Etl.Projects) composes pipelines and so sits above this package, and its option
/// is declared there because only that assembly can name the types it registers. Restricting
/// discovery here would drop that option silently -- it would compile, register nothing, and give
/// no indication why. Cross-assembly options are the norm for a collection others extend;
/// ConnectionTypes works the same way.
/// </summary>
[ExcludeFromCodeCoverage]
[ServiceTypeCollection(
    typeof(PipelineServiceTypeBase),
    typeof(IPipelineServiceType),
    typeof(PipelineServiceTypes),
    ServiceCategory = "PipelineService")]
public partial class PipelineServiceTypes : ServiceTypeCollectionBase<PipelineServiceTypeBase, IPipelineServiceType>
{
    /// <summary>
    /// The connection this domain's configuration rows are read from and written to.
    /// </summary>
    public static string ConfigurationConnection => PlatformConnection.Name;

    /// <summary>
    /// The connection this domain's operational rows live in. The host must set it; there is no default.
    /// </summary>
    /// <remarks>
    /// Deliberately without an initializer, unlike <see cref="ConfigurationConnection"/>. That one may
    /// default because <c>PlatformConfiguration</c> is declared in <c>configurationSchema.json</c> and
    /// is therefore known before any row is read. An operational store is a row INSIDE that store, so a
    /// default here would name a store the application merely hopes exists — the absence the
    /// no-fallbacks rule exists to catch, rather than the ConfigurationConnection case it resembles.
    /// Not proven at registration: every host registers this domain as part of the platform bootstrap,
    /// including hosts that never touch its operational store, so refusing to start them would demand
    /// a value a correct host cannot supply. The sites that read it report it instead.
    /// </remarks>
    public static string? OperationalConnection { get; set; }

    /// <summary>
    /// Sets this collection's Register body: the option collect, then this domain's configuration provider.
    /// </summary>
    static PipelineServiceTypes()
    {
        Registration((builder, loggerFactory) =>
        {
            builder.Services.TryAddSingleton<IPipelineConfigurationProvider>(sp =>
                new PipelineServiceConfigurationProvider(
                    sp.GetRequiredService<ILogger<PipelineServiceConfigurationProvider>>(),
                    sp.GetRequiredService<IConfigurationGatewayProvider>(), PipelineServiceTypes.ConfigurationConnection, "pipe", "Pipeline",
                    ConfigurationPlacementSource.From(sp)));
            builder.Services.TryAddSingleton<PipelineServiceConfigurationProvider>(
                sp => (PipelineServiceConfigurationProvider)sp.GetRequiredService<IPipelineConfigurationProvider>());
            builder.Services.TryAddSingleton<IDomainConfigurationProvider<IPipelineImplementationConfiguration>>(
                sp => sp.GetRequiredService<PipelineServiceConfigurationProvider>());

            // A stored SQL script is its own domain with one implementation, Sql (FDW-1442). It is registered beside the
            // pipeline domain and is read through its own provider; it never appears among the pipelines.
            builder.Services.TryAddSingleton<SqlScriptConfigurationProvider>(sp =>
                new SqlScriptConfigurationProvider(
                    sp.GetRequiredService<ILogger<SqlScriptConfigurationProvider>>(),
                    sp.GetRequiredService<IConfigurationGatewayProvider>(),
                    PipelineServiceTypes.ConfigurationConnection, "pipe", "SqlScriptConfiguration"));
            builder.Services.TryAddSingleton<ScriptConfigurationProvider>(sp =>
            {
                var domain = new ScriptConfigurationProvider(
                    sp.GetRequiredService<ILogger<ScriptConfigurationProvider>>(),
                    sp.GetRequiredService<IConfigurationGatewayProvider>(),
                    PipelineServiceTypes.ConfigurationConnection, "pipe", "Script");
                domain.Register("Sql", sp.GetRequiredService<SqlScriptConfigurationProvider>());
                return domain;
            });
            builder.Services.TryAddSingleton<IScriptConfigurationProvider>(sp => sp.GetRequiredService<ScriptConfigurationProvider>());
            builder.Services.TryAddSingleton<IDomainConfigurationProvider<ISqlScriptImplementationConfiguration>>(
                sp => sp.GetRequiredService<IScriptConfigurationProvider>());

            // A webhook trigger is its own domain with one implementation, Webhook (FDW-1884), registered beside the stored
            // scripts the same way: it is read through its own provider and never appears among the pipelines.
            builder.Services.TryAddSingleton<WebhookTriggerImplementationProvider>(sp =>
                new WebhookTriggerImplementationProvider(
                    sp.GetRequiredService<ILogger<WebhookTriggerImplementationProvider>>(),
                    sp.GetRequiredService<IConfigurationGatewayProvider>(),
                    PipelineServiceTypes.ConfigurationConnection, "pipe", "WebhookTriggerConfiguration"));
            builder.Services.TryAddSingleton<WebhookTriggerConfigurationProvider>(sp =>
            {
                var domain = new WebhookTriggerConfigurationProvider(
                    sp.GetRequiredService<ILogger<WebhookTriggerConfigurationProvider>>(),
                    sp.GetRequiredService<IConfigurationGatewayProvider>(),
                    PipelineServiceTypes.ConfigurationConnection, "pipe", "WebhookTrigger");
                domain.Register("Webhook", sp.GetRequiredService<WebhookTriggerImplementationProvider>());
                return domain;
            });
            builder.Services.TryAddSingleton<IWebhookTriggerConfigurationProvider>(sp => sp.GetRequiredService<WebhookTriggerConfigurationProvider>());
            builder.Services.TryAddSingleton<IDomainConfigurationProvider<IWebhookTriggerImplementationConfiguration>>(
                sp => sp.GetRequiredService<IWebhookTriggerConfigurationProvider>());

            foreach (var option in Options)
            {
                var optionRegistered = option.Register(builder, loggerFactory);
                if (optionRegistered.IsFailure)
                    return optionRegistered;
            }

            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });
    }
}
