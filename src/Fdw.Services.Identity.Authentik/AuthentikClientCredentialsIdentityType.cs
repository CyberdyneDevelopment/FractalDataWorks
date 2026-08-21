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

namespace Fdw.Services.Identity.Authentik;

/// <summary>
/// The Authentik client-credentials identity mechanism — an Authentik Service Account authenticating
/// with a client id and secret.
/// </summary>
[ExcludeFromCodeCoverage]
[ServiceTypeOption(typeof(IdentityServiceTypes), "AuthentikClientCredentials")]
public sealed class AuthentikClientCredentialsIdentityType
    : IdentityServiceTypeBase<IIdentityService, IdentityServiceConfiguration, IIdentityServiceFactory<IIdentityService, IdentityServiceConfiguration>>
{
    /// <summary>Initializes a new instance of the <see cref="AuthentikClientCredentialsIdentityType"/> class.</summary>
    public AuthentikClientCredentialsIdentityType()
        : base("AuthentikClientCredentials", defaultContainerName: "AuthentikClientCredentialsIdentity")
    {
        // Why Append and not Registration: Registration ASSIGNS, discarding whatever body was already
        // installed — including a segment a base constructor prepended. ConnectionTypeBase prepends its
        // factory registration that way, and six connection kinds silently stopped being creatable when
        // their options used Registration (af522f014). This base prepends nothing today, so either is
        // correct right now; Append stays correct if that ever changes.
        Registration((builder, loggerFactory) =>
        {
            var log = loggerFactory?.CreateLogger<AuthentikClientCredentialsIdentityType>()
                ?? NullLogger<AuthentikClientCredentialsIdentityType>.Instance;

            // Why the option registers its own factory: this is the registry the domain provider reads
            // to turn a configuration's ServiceOptionType into something that can build the service. An
            // option that skips it resolves to "No registered service type matches ServiceOptionType"
            // at the first request, which reads like a configuration fault and is not one.
            DefaultServiceProvider<IIdentityService, IdentityServiceConfiguration, IIdentityServiceFactory<IIdentityService, IdentityServiceConfiguration>, IServiceConfigurationProvider<IdentityServiceConfiguration>>
                .Register(Name, sp => new AuthentikClientCredentialsIdentityFactory(
                    sp.GetService<ILoggerFactory>(),
                    sp.GetRequiredService<IHttpClientFactory>().CreateClient(IdentityHttpClient.Name),
                    sp.GetRequiredService<Lazy<IFdwServiceProvider<ISecretManager, SecretManagerConfiguration>>>()));

            // Why registered here: the factory takes the secret-manager provider as a Lazy so it is
            // resolved after the container is built, and nothing else in the graph registers that
            // closed Lazy.
            builder.Services.TryAddScoped(sp => new Lazy<IFdwServiceProvider<ISecretManager, SecretManagerConfiguration>>(
                sp.GetRequiredService<IFdwServiceProvider<ISecretManager, SecretManagerConfiguration>>));

            IdentityHttpClient.Register(builder.Services);

            // The typed body provider, so the header provider can compose the aggregate.

            builder.Services.TryAddSingleton(sp => new AuthentikClientCredentialsConfigurationProvider(

                sp.GetService<ILogger<AuthentikClientCredentialsConfigurationProvider>>()!,

                sp.GetRequiredService<Lazy<IConfigurationGateway>>(),

                invalidator: new Lazy<ICacheInvalidator?>(() => sp.GetService<ICacheInvalidator>())));


            IdentityLog.MechanismRegistered(log, Name);
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });

        Initialization((host, loggerFactory) =>
        {
            // The header provider dispatches on ServiceOptionType to the typed provider registered for
            // it. Without this hand-over the header loads and Configuration stays null.
            var services = host.Services;
            services.GetRequiredService<IdentityServiceConfigurationProvider>()
                .Register(Name, services.GetRequiredService<AuthentikClientCredentialsConfigurationProvider>());

            return GenericResult<IHost>.Success(host);
        });
    }
}
