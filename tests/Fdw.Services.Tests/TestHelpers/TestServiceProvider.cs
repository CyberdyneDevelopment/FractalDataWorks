using System;
using Fdw.Abstractions;
using Fdw.Services;
using Fdw.Services.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Tests.TestHelpers;

/// <summary>
/// A concrete provider for exercising <see cref="PlatformServiceProviderBase{TService, TConfiguration, TFactory, TConfigurationProvider}"/>.
/// </summary>
public sealed class TestServiceProvider
    : PlatformServiceProviderBase<
          IGenericService,
          TestConfiguration,
          IServiceFactory<IGenericService>,
          IDomainConfigurationProvider<TestConfiguration>>
{
    /// <summary>Initializes a new instance of the <see cref="TestServiceProvider"/> class.</summary>
    /// <param name="services">The container this provider resolves factories from.</param>
    /// <param name="logger">The logger for this provider.</param>
    public TestServiceProvider(
        IServiceProvider services,
        ILogger<PlatformServiceProviderBase<IGenericService, TestConfiguration, IServiceFactory<IGenericService>, IDomainConfigurationProvider<TestConfiguration>>> logger)
        : base(services, logger)
    {
    }
}
