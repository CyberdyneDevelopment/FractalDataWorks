using Fdw.Services.Descriptions;
using Fdw.Services.Descriptions.Abstractions;
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
/// ServiceTypeCollection for the services a host gives pipelines (stored scripts, webhook triggers, the owner of a new
/// pipeline). Distinct from <see cref="PipelineTypes"/>, the Pipeline domain, which registers the pipe.Pipeline
/// configuration provider (FDW-6244).
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
    /// Sets this collection's Register body: the owner of a new pipeline, the stored scripts and webhook triggers, then the option collect.
    /// </summary>
    static PipelineServiceTypes()
    {
        Registration((builder, loggerFactory) =>
        {
            // The pipe.Pipeline configuration provider is the Pipeline domain's own and is registered by PipelineTypes (FDW-6244).

            // Decides which organization a new pipeline belongs to (FDW-4573). Scoped: it reads the caller's ambient authentication context and the scoped organization scope.
            builder.Services.TryAddScoped<PipelineOwner>(sp =>
                new PipelineOwner(
                    sp.GetRequiredService<Fdw.Services.Authentication.Abstractions.Security.IAuthenticationContextAccessor>(),
                    sp.GetRequiredService<Fdw.Services.Authorization.Abstractions.IOrganizationScope>(),
                    sp.GetRequiredService<Fdw.Services.Multitenancy.Abstractions.IOrganizationProvider>(),
                    sp.GetService<ILogger<PipelineOwner>>()));

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
                    PipelineServiceTypes.ConfigurationConnection, "pipe", "Script",
                    sp.GetRequiredService<IDescriptionWriter>(), sp.GetRequiredService<IDescriptionReader>());
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
                    PipelineServiceTypes.ConfigurationConnection, "pipe", "WebhookTrigger",
                    sp.GetRequiredService<IDescriptionWriter>(), sp.GetRequiredService<IDescriptionReader>());
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
