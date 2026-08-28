using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Fdw.Services.Abstractions;
using Fdw.Services.Hosting.Abstractions;

namespace Fdw.Services.Hosting;

/// <summary>
/// The hosting domain's service provider.
/// </summary>
public sealed class HostingServiceProvider
    : PlatformServiceProviderBase<
        IHostingService,
        IHostingImplementationConfiguration,
        IHostingFactory<IHostingService, IHostingImplementationConfiguration>,
        IHostingConfigurationProvider>,
      IHostingServiceProvider
{
    /// <summary>Initializes a new instance of the <see cref="HostingServiceProvider"/> class.</summary>
    /// <param name="services">The container this provider resolves its factories from.</param>
    /// <param name="logger">The logger for this provider.</param>
    public HostingServiceProvider(IServiceProvider services, ILogger<HostingServiceProvider> logger)
        : base(services, logger ?? NullLogger<HostingServiceProvider>.Instance)
    {
    }
}
