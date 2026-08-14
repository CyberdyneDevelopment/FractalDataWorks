using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using Fdw.Collections;
using Fdw.Results;
using Fdw.ServiceTypes;
using Fdw.Services.Abstractions;
using Fdw.Services.Identity.Abstractions;
using Fdw.Services.Identity.Logging;
using Microsoft.Extensions.DependencyInjection;
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
        Registration((builder, loggerFactory) =>
        {
            var log = loggerFactory?.CreateLogger<AuthentikJwtFederationIdentityType>()
                ?? NullLogger<AuthentikJwtFederationIdentityType>.Instance;

            // Why the option registers its own factory: see AuthentikClientCredentialsIdentityType —
            // an option that skips this resolves to "No registered service type matches
            // ServiceOptionType" at the first request.
            DefaultServiceProvider<IIdentityService, IdentityServiceConfiguration, IIdentityServiceFactory<IIdentityService, IdentityServiceConfiguration>, IServiceConfigurationProvider<IdentityServiceConfiguration>>
                .Register(Name, sp => new AuthentikJwtFederationIdentityFactory(
                    sp.GetService<ILoggerFactory>(),
                    sp.GetRequiredService<IHttpClientFactory>().CreateClient(AuthentikHttpClient.Name)));

            AuthentikHttpClient.Register(builder.Services);

            IdentityLog.MechanismRegistered(log, Name);
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });
    }
}
