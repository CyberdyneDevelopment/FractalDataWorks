using System;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using Fdw.Collections;
using Fdw.Results;
using Fdw.ServiceTypes;
using Fdw.Services.Abstractions;
using Fdw.Services.Identity;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Identity.Abstractions;
using Fdw.Services.Identity.Logging;
using Fdw.ServiceTypes.Logging;
using Fdw.Services.SecretManagers;
using Fdw.Services.SecretManagers.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Identity.ClientCredentials;

/// <summary>
/// The client-credentials identity mechanism — a service authenticating to a token endpoint with a
/// client id and secret, for service-to-service calls that have no user in the loop.
/// </summary>
[ExcludeFromCodeCoverage]
[Implementation(typeof(IdentityServiceTypes), "ClientCredentials")]
public sealed class ClientCredentialsIdentityType
    : IdentityServiceTypeBase<IIdentityService, IIdentityServiceImplementationConfiguration, IIdentityServiceFactory<IIdentityService, IIdentityServiceImplementationConfiguration>>
{
    /// <summary>Gets or sets the domain this implementation belongs to.</summary>
    /// <remarks>Set by the provider from the domain row; never persisted.</remarks>
    public string Domain { get; set; } = string.Empty;

    /// <inheritdoc/>
    /// <remarks>Set by the provider from the domain row; never persisted.</remarks>
    public string Implementation { get; set; } = string.Empty;

    /// <summary>Initializes a new instance of the <see cref="ClientCredentialsIdentityType"/> class.</summary>
    public ClientCredentialsIdentityType()
        : base("ClientCredentials", defaultContainerName: "ClientCredentialsIdentity")
    {
        Registration((builder, loggerFactory) =>
        {
            var log = loggerFactory?.CreateLogger<ClientCredentialsIdentityType>()
                ?? NullLogger<ClientCredentialsIdentityType>.Instance;

            builder.Services.TryAdd(new ServiceDescriptor(
                typeof(IClientCredentialsIdentityFactory),
                sp => new ClientCredentialsIdentityFactory(
                    sp.GetService<ILoggerFactory>(),
                    sp.GetRequiredService<IHttpClientFactory>().CreateClient(IdentityHttpClient.Name),
                    sp.GetRequiredService<ISecretManagerProvider>()),
                ServiceLifetime.Transient));
            builder.Services.TryAdd(new ServiceDescriptor(
                typeof(IClientCredentialsIdentityProvider),
                sp => new ClientCredentialsIdentityProvider(sp.GetRequiredService<IClientCredentialsIdentityFactory>()),
                ServiceLifetime.Transient));

            // The typed body provider, so the header provider can compose the aggregate. Registration
            // only makes it resolvable; Initialization is where it is handed over, because the header
            // provider has to exist first.
            builder.Services.AddSingleton<IClientCredentialsConfigurationProvider, ClientCredentialsConfigurationProvider>(sp => new ClientCredentialsConfigurationProvider(sp.GetRequiredService<ILogger<ClientCredentialsConfigurationProvider>>(), sp.GetRequiredService<IConfigurationGatewayProvider>(), IdentityServiceTypes.ConfigurationConnection));

            IdentityHttpClient.Register(builder.Services);

            IdentityLog.MechanismRegistered(log, Name);
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });

        // Called once per IdentityServiceTypes AddScoped construction, with THAT construction's own
        // serviceProvider — so whichever scope actually builds domainProvider (root at startup, a
        // real request's own scope for a request) is the same scope this closure resolves against.
        Registration((serviceProvider, domainProvider, domainConfigurationProvider, logger) =>
        {
            if (domainConfigurationProvider is not null)
                domainConfigurationProvider.Register(Name, serviceProvider.GetRequiredService<IClientCredentialsConfigurationProvider>());

            if (domainProvider is null)
                return GenericResult.Success();

            var factoryResult = domainProvider.Register(Name, () => serviceProvider.GetRequiredService<IClientCredentialsIdentityProvider>());
            if (!factoryResult.IsSuccess)
            {
                ServiceTypeLog.OptionFactoryRegistrationFailed(
                    logger, nameof(ClientCredentialsIdentityType), Name, nameof(IClientCredentialsIdentityProvider), factoryResult.CurrentMessage);
                return factoryResult;
            }

            ServiceTypeLog.OptionFactoryRegistered(logger, nameof(ClientCredentialsIdentityType), Name, nameof(IClientCredentialsIdentityProvider));
            return factoryResult;
        });
    }
}
