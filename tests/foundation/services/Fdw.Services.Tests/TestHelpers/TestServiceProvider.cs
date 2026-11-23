using System;
using Fdw.Abstractions;
using Fdw.Services;
using Fdw.Services.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Tests.TestHelpers;

/// <summary>
/// A concrete provider for exercising <see cref="DomainServiceProviderBase{TService, TConfiguration, TFactory, TConfigurationProvider}"/>.
/// </summary>
public sealed class TestServiceProvider
    : DomainServiceProviderBase<
          IGenericService,
          TestConfiguration,
          IServiceFactory<IGenericService>,
          IDomainConfigurationProvider<TestConfiguration>>
{
    /// <summary>Gets how this provider reads the configuration it builds a service from.</summary>
    /// <remarks>This domain has no description fill today, so its configuration is read whole (FDW-7354).</remarks>
    protected override ConfigurationReadOptions ReadOptions => ConfigurationReadOptions.Whole;

    /// <summary>Initializes a new instance of the <see cref="TestServiceProvider"/> class.</summary>
    /// <param name="logger">The logger for this provider.</param>
    public TestServiceProvider(
        ILogger<DomainServiceProviderBase<IGenericService, TestConfiguration, IServiceFactory<IGenericService>, IDomainConfigurationProvider<TestConfiguration>>> logger)
        : base(logger)
    {
    }
}
