using System;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using Fdw.Collections;
using Fdw.Results;
using Fdw.ServiceTypes;
using Fdw.Services.Abstractions;
using Fdw.Services.Identity.Abstractions;
using Fdw.Services.Identity.Logging;
using Fdw.Services.SecretManagers;
using Fdw.Services.SecretManagers.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Identity.FdwOpenIddict;

/// <summary>
/// The FDW OpenIddict identity mechanism — an FDW service authenticating to FDW's own authorization
/// server with a client id and secret, for service-to-service calls inside the deployment.
/// </summary>
[ExcludeFromCodeCoverage]
[ServiceTypeOption(typeof(IdentityServiceTypes), "FdwOpenIddict")]
public sealed class FdwOpenIddictIdentityType
    : IdentityServiceTypeBase<IIdentityService, IdentityServiceConfiguration, IIdentityServiceFactory<IIdentityService, IdentityServiceConfiguration>>
{
    /// <summary>Initializes a new instance of the <see cref="FdwOpenIddictIdentityType"/> class.</summary>
    public FdwOpenIddictIdentityType()
        : base("FdwOpenIddict", defaultContainerName: "FdwOpenIddictIdentity")
    {
        // Why Append and not Registration: Registration ASSIGNS, discarding whatever body was already
        // installed — including a segment a base constructor prepended. ConnectionTypeBase prepends its
        // factory registration that way, and six connection kinds silently stopped being creatable when
        // their options used Registration (af522f014). This base prepends nothing today, so either is
        // correct right now; Append stays correct if that ever changes.
        AppendRegistration((builder, loggerFactory) =>
        {
            var log = loggerFactory?.CreateLogger<FdwOpenIddictIdentityType>()
                ?? NullLogger<FdwOpenIddictIdentityType>.Instance;

            // Why the option registers its own factory: this is the registry the domain provider reads
            // to turn a configuration's ServiceOptionType into something that can build the service. An
            // option that skips it resolves to "No registered service type matches ServiceOptionType"
            // at the first request, which reads like a configuration fault and is not one.
            DefaultServiceProvider<IIdentityService, IdentityServiceConfiguration, IIdentityServiceFactory<IIdentityService, IdentityServiceConfiguration>, IServiceConfigurationProvider<IdentityServiceConfiguration>>
                .Register(Name, sp => new FdwOpenIddictIdentityFactory(
                    sp.GetService<ILoggerFactory>(),
                    sp.GetRequiredService<IHttpClientFactory>().CreateClient(IdentityHttpClient.Name),
                    sp.GetRequiredService<Lazy<IFdwServiceProvider<ISecretManager, SecretManagerConfiguration>>>()));

            // Why registered here: the factory takes the secret-manager provider as a Lazy so it is
            // resolved after the container is built, and nothing else in the graph registers that
            // closed Lazy.
            builder.Services.TryAddScoped(sp => new Lazy<IFdwServiceProvider<ISecretManager, SecretManagerConfiguration>>(
                sp.GetRequiredService<IFdwServiceProvider<ISecretManager, SecretManagerConfiguration>>));

            IdentityHttpClient.Register(builder.Services);

            IdentityLog.MechanismRegistered(log, Name);
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });
    }
}
