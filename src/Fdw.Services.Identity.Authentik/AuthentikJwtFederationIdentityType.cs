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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Identity.Authentik;

/// <summary>
/// The Authentik federated-JWT identity mechanism — exchanging an assertion an external OIDC issuer
/// already minted for this workload, with no static secret anywhere.
/// </summary>
[ExcludeFromCodeCoverage]
[ServiceTypeOption(typeof(IdentityServiceTypes), "AuthentikJwtFederation")]
public sealed class AuthentikJwtFederationIdentityType
    : IdentityServiceTypeBase<IIdentityService, IdentityServiceConfiguration, IIdentityServiceFactory<IIdentityService, IdentityServiceConfiguration>>
{
    /// <summary>Initializes a new instance of the <see cref="AuthentikJwtFederationIdentityType"/> class.</summary>
    public AuthentikJwtFederationIdentityType()
        : base("AuthentikJwtFederation", defaultContainerName: "AuthentikJwtFederationIdentity")
    {
        // Why Append and not Registration: Registration ASSIGNS, discarding whatever body was already
        // installed — including a segment a base constructor prepended. ConnectionTypeBase prepends its
        // factory registration that way, and six connection kinds silently stopped being creatable when
        // their options used Registration (af522f014). This base prepends nothing today, so either is
        // correct right now; Append stays correct if that ever changes.
        AppendRegistration((builder, loggerFactory) =>
        {
            var log = loggerFactory?.CreateLogger<AuthentikJwtFederationIdentityType>()
                ?? NullLogger<AuthentikJwtFederationIdentityType>.Instance;

            // Why the option registers its own factory: see AuthentikClientCredentialsIdentityType —
            // an option that skips this resolves to "No registered service type matches
            // ServiceOptionType" at the first request.
            DefaultServiceProvider<IIdentityService, IdentityServiceConfiguration, IIdentityServiceFactory<IIdentityService, IdentityServiceConfiguration>, IServiceConfigurationProvider<IdentityServiceConfiguration>>
                .Register(Name, sp => new AuthentikJwtFederationIdentityFactory(
                    sp.GetService<ILoggerFactory>(),
                    sp.GetRequiredService<IHttpClientFactory>().CreateClient(IdentityHttpClient.Name)));

            IdentityHttpClient.Register(builder.Services);

            // The typed body provider, so the header provider can compose the aggregate.

            builder.Services.TryAddSingleton(sp => new AuthentikJwtFederationConfigurationProvider(

                sp.GetService<ILogger<AuthentikJwtFederationConfigurationProvider>>()!,

                sp.GetRequiredService<Lazy<IConfigurationGateway>>(),

                invalidator: new Lazy<ICacheInvalidator?>(() => sp.GetService<ICacheInvalidator>())));


            IdentityLog.MechanismRegistered(log, Name);
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });

        AppendInitialization((host, loggerFactory) =>
        {
            // The header provider dispatches on ServiceOptionType to the typed provider registered for
            // it. Without this hand-over the header loads and Configuration stays null.
            var services = host.Services;
            services.GetRequiredService<IdentityServiceConfigurationProvider>()
                .Register(Name, services.GetRequiredService<AuthentikJwtFederationConfigurationProvider>());

            return GenericResult<IHost>.Success(host);
        });
    }
}
