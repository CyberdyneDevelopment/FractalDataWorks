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

        Initialization((host, loggerFactory) =>
        {
            var services = host.Services;
            services.GetRequiredService<IIdentityServiceConfigurationProvider>()
                .Register(Name, services.GetRequiredService<IClientCredentialsConfigurationProvider>());

            IdentityLog.MechanismRegistered(
                loggerFactory?.CreateLogger<ClientCredentialsIdentityType>()
                    ?? NullLogger<ClientCredentialsIdentityType>.Instance,
                Name);

            return GenericResult<IHost>.Success(host);
        });
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Why this replaces what used to be here in <c>Initialization</c>: that callback resolved
    /// <see cref="IIdentityServiceProvider"/> from the ROOT container once, at startup, and this
    /// domain provider is registered <c>AddScoped</c> — so root's copy is one instance among many
    /// a real request never sees, and a real request's own instance never had this call reach it.
    /// This method is instead called once per construction, by <c>IdentityServiceTypes</c>'s own
    /// factory, and handed THAT construction's <paramref name="serviceProvider"/> — the same
    /// scope root or a request actually used to build <paramref name="domainProvider"/> itself.
    /// </remarks>
    public override IGenericResult RegisterImplementationProvider(IIdentityServiceProvider domainProvider, IServiceProvider serviceProvider, ILogger logger)
    {
        var factoryResult = domainProvider.Register(Name, () => serviceProvider.GetRequiredService<IClientCredentialsIdentityProvider>());
        if (!factoryResult.IsSuccess)
        {
            ServiceTypeLog.OptionFactoryRegistrationFailed(
                logger, nameof(ClientCredentialsIdentityType), Name, nameof(IClientCredentialsIdentityProvider), factoryResult.CurrentMessage);
            return factoryResult;
        }

        ServiceTypeLog.OptionFactoryRegistered(logger, nameof(ClientCredentialsIdentityType), Name, nameof(IClientCredentialsIdentityProvider));
        return factoryResult;
    }
}
