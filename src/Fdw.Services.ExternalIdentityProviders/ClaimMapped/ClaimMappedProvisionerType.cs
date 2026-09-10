using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Authorization;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.ExternalIdentityProviders.Abstractions;
using Fdw.Services.ExternalIdentityProviders.Binding;
using Fdw.Services.ExternalIdentityProviders.Logging;
using Fdw.Services.Users;
using Fdw.ServiceTypes;
using Fdw.ServiceTypes.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.ExternalIdentityProviders.ClaimMapped;

/// <summary>
/// ClaimMapped <see cref="ExternalIdentityProvisionerTypes"/> Implementation. Registers the header
/// + typed-body gateway-backed configuration providers and the
/// <see cref="ClaimMappedProvisionerFactory"/> that builds <see cref="ClaimMappedProvisioner"/>
/// instances. Mirrors <see cref="Chained.ChainedExternalIdentityProvisionerType"/> structurally.
/// </summary>
[ExcludeFromCodeCoverage]
[Implementation(typeof(ExternalIdentityProvisionerTypes), "ClaimMapped")]
public sealed class ClaimMappedProvisionerType
    : ExternalIdentityProvisionerTypeBase<
        IExternalIdentityProvisioner,
        IExternalIdentityProvisionerImplementationConfiguration,
        IExternalIdentityProvisionerFactory<IExternalIdentityProvisioner, IExternalIdentityProvisionerImplementationConfiguration>>
{
    /// <summary>Initializes a new instance of <see cref="ClaimMappedProvisionerType"/>.</summary>
    public ClaimMappedProvisionerType() : base(name: "ClaimMapped", defaultContainerName: "ExternalIdentityProvisioner")
    {
        Registration((builder, loggerFactory) =>
        {
            builder.Services.TryAddSingleton<ClaimMappedExternalIdentityProvisionerConfigurationProvider>(sp => new ClaimMappedExternalIdentityProvisionerConfigurationProvider(sp.GetRequiredService<ILogger<ClaimMappedExternalIdentityProvisionerConfigurationProvider>>(), sp.GetRequiredService<IConfigurationGatewayProvider>(), ExternalIdentityProvisionerTypes.ConfigurationConnection));
            builder.Services.TryAddSingleton<IClaimMappedExternalIdentityProvisionerConfigurationProvider>(sp => sp.GetRequiredService<ClaimMappedExternalIdentityProvisionerConfigurationProvider>());

            ExternalIdentityProvisionerServiceProvider.Register<IClaimMappedProvisionerFactory, ClaimMappedProvisionerFactory>(
                builder, Name, ServiceLifetime.Scoped);

            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });

        Initialization((host, hostLoggerFactory) =>
        {
            var services = host.Services;
            var provider = services.GetRequiredService<IPlatformServiceProvider<IExternalIdentityProvisioner, IExternalIdentityProvisionerImplementationConfiguration>>();

            var loggerFactory = services.GetService<ILoggerFactory>() ?? NullLoggerFactory.Instance;
            var logger = loggerFactory.CreateLogger<ClaimMappedProvisionerType>();

            var factory = services.GetRequiredService<IExternalIdentityProvisionerFactory<IExternalIdentityProvisioner, IExternalIdentityProvisionerImplementationConfiguration>>();
            var domainProvider = services.GetRequiredService<IExternalIdentityProvisionerConfigurationProvider>();
            var typedProvider = services.GetRequiredService<IClaimMappedExternalIdentityProvisionerConfigurationProvider>();

            domainProvider.Register("ClaimMapped", typedProvider);

            var factoryResult = provider.Register("ClaimMapped", factory);
            if (!factoryResult.IsSuccess) return factoryResult.ToNewResult<IHost>();

            ServiceTypeLog.OptionFactoryRegistered(
                logger, nameof(ClaimMappedProvisionerType), Name, factory.GetType().Name);

            ExternalIdentityProvisionerLog.ProviderRegistered(logger, "ClaimMapped");

            return GenericResult<IHost>.Success(host);
        });
    }
}
