using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
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

            // Decorate the domain's own AddScoped<IIdentityServiceProvider> registration -- already
            // in builder.Services by the time this runs, since IdentityServiceTypes' own Registration
            // body sets it up before running the option collect.
            var existing = builder.Services.Single(d => d.ServiceType == typeof(IIdentityServiceProvider));
            builder.Services.Remove(existing);
            builder.Services.AddScoped<IIdentityServiceProvider>(sp =>
            {
                var provider = (IIdentityServiceProvider)existing.ImplementationFactory!(sp);

                var factoryResult = provider.Register(Name, () => sp.GetRequiredService<IClientCredentialsIdentityProvider>());
                if (!factoryResult.IsSuccess)
                {
                    ServiceTypeLog.OptionFactoryRegistrationFailed(
                        sp.GetService<ILoggerFactory>()?.CreateLogger<ClientCredentialsIdentityType>() ?? NullLogger<ClientCredentialsIdentityType>.Instance,
                        nameof(ClientCredentialsIdentityType), Name, nameof(IClientCredentialsIdentityProvider), factoryResult.CurrentMessage);
                }

                return provider;
            });

            IdentityLog.MechanismRegistered(log, Name);
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });

        // IIdentityServiceConfigurationProvider is Singleton, so root's Initialize call reaches the
        // same instance every later resolution sees -- unlike the domain SERVICE provider above,
        // this one is safe to populate once, here, unmodified.
        Initialization((host, loggerFactory) =>
        {
            var services = host.Services;
            services.GetRequiredService<IIdentityServiceConfigurationProvider>()
                .Register(Name, services.GetRequiredService<IClientCredentialsConfigurationProvider>());
            return GenericResult<IHost>.Success(host);
        });
    }
}
