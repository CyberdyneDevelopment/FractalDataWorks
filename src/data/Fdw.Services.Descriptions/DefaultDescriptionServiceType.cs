using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Descriptions.Abstractions;
using Fdw.Services.Descriptions.Logging;
using Fdw.Services.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Descriptions;

/// <summary>
/// Registers the descriptions domain: the domain and implementation providers over <c>catalog.Description</c> and <c>catalog.DescriptionConfiguration</c>, and the
/// <see cref="IDescriptionService"/> the owning domains write through.
/// </summary>
[ExcludeFromCodeCoverage]
[Implementation(typeof(DescriptionServiceTypes), "Default")]
public sealed class DefaultDescriptionServiceType : DescriptionServiceTypeBase
{
    private const string CatalogPath = "catalog";
    private const string DomainName = "Description";

    /// <summary>Initializes a new instance of the <see cref="DefaultDescriptionServiceType"/> class.</summary>
    public DefaultDescriptionServiceType()
        : base(
            "Default",
            "Descriptions:Default",
            "Default Descriptions Services",
            "The descriptions providers and the service that saves, deletes and reads a description",
            "Descriptions")
    {
        Registration((builder, loggerFactory) =>
        {
            // A part of no characters holds no text: refused here, where a host sets it, and not found later as a save that never ends (FDW-6123).
            if (DescriptionServiceTypes.PartLength < 1)
            {
                return GenericResult<IHostApplicationBuilder>.Failure(
                    DescriptionsLog.PartLengthInvalid(
                        loggerFactory?.CreateLogger<DefaultDescriptionServiceType>() ?? NullLogger<DefaultDescriptionServiceType>.Instance,
                        DescriptionServiceTypes.PartLength));
            }

            builder.Services.AddSingleton<IDescriptionImplementationConfigurationProvider, DescriptionImplementationConfigurationProvider>(sp => new DescriptionImplementationConfigurationProvider(
                sp.GetRequiredService<ILogger<DescriptionImplementationConfigurationProvider>>(),
                sp.GetRequiredService<IConfigurationGatewayProvider>(),
                DescriptionServiceTypes.ConfigurationConnection, CatalogPath, "DescriptionConfiguration"));

            builder.Services.TryAddSingleton<DescriptionConfigurationProvider>(sp =>
            {
                var domain = new DescriptionConfigurationProvider(
                    sp.GetRequiredService<ILogger<DescriptionConfigurationProvider>>(),
                    sp.GetRequiredService<IConfigurationGatewayProvider>(),
                    DescriptionServiceTypes.ConfigurationConnection, CatalogPath, DomainName);
                domain.Register(DomainName, sp.GetRequiredService<IDescriptionImplementationConfigurationProvider>());
                return domain;
            });
            builder.Services.TryAddSingleton<IDescriptionConfigurationProvider>(sp => sp.GetRequiredService<DescriptionConfigurationProvider>());

            // One singleton behind both contracts: the service reads the calling principal through the singleton accessor, so the providers of the owning
            // domains, which are singletons, can hold the writer (FDW-6121).
            builder.Services.TryAddSingleton<DescriptionService>(sp => new DescriptionService(
                sp.GetRequiredService<IDescriptionConfigurationProvider>(),
                sp.GetRequiredService<IEffectiveSettingsProvider>(),
                sp.GetRequiredService<IConfigurationGatewayProvider>(),
                sp.GetRequiredService<IAuthenticationContextAccessor>(),
                DescriptionServiceTypes.PartLength,
                sp.GetRequiredService<ILogger<DescriptionService>>()));
            builder.Services.TryAddSingleton<IDescriptionService>(sp => sp.GetRequiredService<DescriptionService>());
            builder.Services.TryAddSingleton<IDescriptionWriter>(sp => sp.GetRequiredService<DescriptionService>());
            builder.Services.TryAddSingleton<IDescriptionReader>(sp => sp.GetRequiredService<DescriptionService>());

            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });

        // Every description type states the permission that reads a thing of it: checked here, where a refusal stops the host with a message (FDW-6208).
        Initialization((host, loggerFactory) =>
        {
            var stated = DescriptionTypeRequirements.ReadPermissionsStated(
                DescriptionTypes.All(), (loggerFactory ?? NullLoggerFactory.Instance).CreateLogger<DefaultDescriptionServiceType>());
            return stated.IsFailure ? stated.ToNewResult<IHost>() : GenericResult<IHost>.Success(host);
        });
    }
}
