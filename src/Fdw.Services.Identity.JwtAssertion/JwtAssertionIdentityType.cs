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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Identity.JwtAssertion;

/// <summary>
/// The JWT-assertion identity mechanism — exchanging an assertion an external OIDC issuer
/// already minted for this workload, with no static secret anywhere.
/// </summary>
[ExcludeFromCodeCoverage]
[Implementation(typeof(IdentityServiceTypes), "JwtAssertion")]
public sealed class JwtAssertionIdentityType
    : IdentityServiceTypeBase<IIdentityService, IIdentityServiceImplementationConfiguration, IIdentityServiceFactory<IIdentityService, IIdentityServiceImplementationConfiguration>>
{
    /// <summary>Gets or sets the domain this implementation belongs to.</summary>
    /// <remarks>Set by the provider from the domain row; never persisted.</remarks>
    public string Domain { get; set; } = string.Empty;

    /// <inheritdoc/>
    /// <remarks>Set by the provider from the domain row; never persisted.</remarks>
    public string Implementation { get; set; } = string.Empty;

    /// <summary>Initializes a new instance of the <see cref="JwtAssertionIdentityType"/> class.</summary>
    public JwtAssertionIdentityType()
        : base("JwtAssertion", defaultContainerName: "JwtAssertionIdentity")
    {
        Registration((builder, loggerFactory) =>
        {
            var log = loggerFactory?.CreateLogger<JwtAssertionIdentityType>()
                ?? NullLogger<JwtAssertionIdentityType>.Instance;

            builder.Services.TryAdd(new ServiceDescriptor(
                typeof(IJwtAssertionIdentityFactory),
                sp => new JwtAssertionIdentityFactory(
                    sp.GetService<ILoggerFactory>(),
                    sp.GetRequiredService<IHttpClientFactory>().CreateClient(IdentityHttpClient.Name)),
                ServiceLifetime.Transient));
            builder.Services.TryAdd(new ServiceDescriptor(
                typeof(IJwtAssertionIdentityProvider),
                sp => new JwtAssertionIdentityProvider(sp.GetRequiredService<IJwtAssertionIdentityFactory>()),
                ServiceLifetime.Transient));

            IdentityHttpClient.Register(builder.Services);

            // The typed body provider, so the header provider can compose the aggregate.

            builder.Services.AddSingleton<IJwtAssertionConfigurationProvider, JwtAssertionConfigurationProvider>(sp => new JwtAssertionConfigurationProvider(sp.GetRequiredService<ILogger<JwtAssertionConfigurationProvider>>(), sp.GetRequiredService<IConfigurationGatewayProvider>(), IdentityServiceTypes.ConfigurationConnection));

            // Decorate the domain's own AddScoped<IIdentityServiceProvider> registration -- already
            // in builder.Services by the time this runs, since IdentityServiceTypes' own Registration
            // body sets it up before running the option collect.
            var existing = builder.Services.Single(d => d.ServiceType == typeof(IIdentityServiceProvider));
            builder.Services.Remove(existing);
            builder.Services.AddScoped<IIdentityServiceProvider>(sp =>
            {
                var provider = (IIdentityServiceProvider)existing.ImplementationFactory!(sp);

                var factoryResult = provider.Register(Name, () => sp.GetRequiredService<IJwtAssertionIdentityProvider>());
                if (!factoryResult.IsSuccess)
                {
                    ServiceTypeLog.OptionFactoryRegistrationFailed(
                        sp.GetService<ILoggerFactory>()?.CreateLogger<JwtAssertionIdentityType>() ?? NullLogger<JwtAssertionIdentityType>.Instance,
                        nameof(JwtAssertionIdentityType), Name, nameof(IJwtAssertionIdentityProvider), factoryResult.CurrentMessage);
                }

                return provider;
            });

            IdentityLog.MechanismRegistered(log, Name);
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });

        // The header provider dispatches on Implementation to the typed provider registered for it,
        // so this Initialize call is what makes Configuration resolve at all. Safe unmodified: the
        // domain configuration provider is Singleton, so root's call reaches the same instance every
        // later resolution sees.
        Initialization((host, loggerFactory) =>
        {
            var services = host.Services;
            services.GetRequiredService<IIdentityServiceConfigurationProvider>()
                .Register(Name, services.GetRequiredService<IJwtAssertionConfigurationProvider>());
            return GenericResult<IHost>.Success(host);
        });
    }
}
