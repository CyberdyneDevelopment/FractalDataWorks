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
[ServiceTypeOption(typeof(IdentityServiceTypes), "ClientCredentials")]
public sealed class ClientCredentialsIdentityType
    : IdentityServiceTypeBase<IIdentityService, IIdentityServiceImplementationConfiguration, IIdentityServiceFactory<IIdentityService, IIdentityServiceImplementationConfiguration>>
{
    /// <summary>Initializes a new instance of the <see cref="ClientCredentialsIdentityType"/> class.</summary>
    public ClientCredentialsIdentityType()
        : base("ClientCredentials", defaultContainerName: "ClientCredentialsIdentity")
    {
        Registration((builder, loggerFactory) =>
        {
            var log = loggerFactory?.CreateLogger<ClientCredentialsIdentityType>()
                ?? NullLogger<ClientCredentialsIdentityType>.Instance;

            IdentityServiceProvider
                .Register(Name, sp => new ClientCredentialsIdentityFactory(
                    sp.GetService<ILoggerFactory>(),
                    sp.GetRequiredService<IHttpClientFactory>().CreateClient(IdentityHttpClient.Name),
                    sp.GetRequiredService<Lazy<ISecretManagerProvider>>()));

            builder.Services.TryAddScoped(sp => new Lazy<ISecretManagerProvider>(
                sp.GetRequiredService<ISecretManagerProvider>));

            // The typed body provider, so the header provider can compose the aggregate. Registration
            // only makes it resolvable; Initialization is where it is handed over, because the header
            // provider has to exist first.
            builder.Services.TryAddSingleton(sp => new ClientCredentialsConfigurationProvider(
                sp.GetService<ILogger<ClientCredentialsConfigurationProvider>>()!,
                sp.GetRequiredService<IConfigurationGatewayProvider>(),
                    IdentityServiceTypes.ConfigurationConnection));

            IdentityHttpClient.Register(builder.Services);

            IdentityLog.MechanismRegistered(log, Name);
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });

        Initialization((host, loggerFactory) =>
        {
            var services = host.Services;
            services.GetRequiredService<IIdentityServiceConfigurationProvider>()
                .Register(Name, services.GetRequiredService<ClientCredentialsConfigurationProvider>());

            IdentityLog.MechanismRegistered(
                loggerFactory?.CreateLogger<ClientCredentialsIdentityType>()
                    ?? NullLogger<ClientCredentialsIdentityType>.Instance,
                Name);

            return GenericResult<IHost>.Success(host);
        });
    }
}
