using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Fdw.Services.Abstractions;
using Fdw.Services.Hosts.Abstractions;

namespace Fdw.Services.Hosts;

/// <summary>
/// The hosting domain's service provider.
/// </summary>
public sealed class HostServiceProvider
    : PlatformServiceProviderBase<
        IHostService,
        IHostImplementationConfiguration,
        IHostFactory<IHostService, IHostImplementationConfiguration>,
        IHostConfigurationProvider>,
      IHostServiceProvider
{
    /// <summary>Initializes a new instance of the <see cref="HostServiceProvider"/> class.</summary>
    /// <param name="services">The container this provider resolves its factories from.</param>
    /// <param name="logger">The logger for this provider.</param>
    public HostServiceProvider(IServiceProvider services, ILogger<HostServiceProvider> logger)
        : base(services, logger ?? NullLogger<HostServiceProvider>.Instance)
    {
    }
}
