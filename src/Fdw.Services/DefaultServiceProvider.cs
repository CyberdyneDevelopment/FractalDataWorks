using System;
using Microsoft.Extensions.Logging;
using Fdw.Abstractions;
using Fdw.Configuration;
using Fdw.Services.Abstractions;

namespace Fdw.Services;

/// <summary>
/// The pre-named-provider shape, kept only until the collections still naming it as their
/// <c>ProviderType</c> have their own.
/// </summary>
/// <typeparam name="TService">The service this provider resolves.</typeparam>
/// <typeparam name="TConfiguration">The configuration that service binds to.</typeparam>
/// <typeparam name="TFactory">The factory that builds the service.</typeparam>
/// <typeparam name="TConfigurationProvider">The provider that supplies the typed configuration.</typeparam>
/// <remarks>
/// Nothing new should name this. A domain gets its own named provider deriving from
/// <see cref="PlatformServiceProviderBase{TService, TConfiguration, TFactory, TConfigurationProvider}"/>
/// directly; when the last collection stops naming this type, delete the file.
/// </remarks>
public class DefaultServiceProvider<TService, TConfiguration, TFactory, TConfigurationProvider>
    : PlatformServiceProviderBase<TService, TConfiguration, TFactory, TConfigurationProvider>
    where TService : IGenericService
    where TConfiguration : class, IGenericConfiguration
    where TFactory : IServiceFactory<TService>
    where TConfigurationProvider : IServiceConfigurationProvider<TConfiguration>
{
    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="DefaultServiceProvider{TService, TConfiguration, TFactory, TConfigurationProvider}"/> class.
    /// </summary>
    /// <param name="services">The scope's container, used to resolve the registered factories.</param>
    /// <param name="logger">The logger for this provider.</param>
    public DefaultServiceProvider(
        IServiceProvider services,
        ILogger<PlatformServiceProviderBase<TService, TConfiguration, TFactory, TConfigurationProvider>> logger)
        : base(services, logger)
    {
    }
}
