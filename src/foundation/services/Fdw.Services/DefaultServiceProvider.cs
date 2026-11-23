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
/// <see cref="DomainServiceProviderBase{TService, TConfiguration, TFactory, TConfigurationProvider}"/>
/// directly; when the last collection stops naming this type, delete the file.
/// </remarks>
public class DefaultServiceProvider<TService, TConfiguration, TFactory, TConfigurationProvider>
    : DomainServiceProviderBase<TService, TConfiguration, TFactory, TConfigurationProvider>
    where TService : IGenericService
    where TConfiguration : class, IImplementationConfiguration
    where TFactory : IServiceFactory<TService>
    where TConfigurationProvider : IDomainConfigurationProvider<TConfiguration>
{
    /// <summary>Gets how this provider reads the configuration it builds a service from.</summary>
    /// <remarks>This domain has no description fill today, so its configuration is read whole (FDW-7354).</remarks>
    protected override ConfigurationReadOptions ReadOptions => ConfigurationReadOptions.Whole;

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="DefaultServiceProvider{TService, TConfiguration, TFactory, TConfigurationProvider}"/> class.
    /// </summary>
    /// <param name="logger">The logger for this provider.</param>
    public DefaultServiceProvider(
        ILogger<DomainServiceProviderBase<TService, TConfiguration, TFactory, TConfigurationProvider>> logger)
        : base(logger)
    {
    }
}
