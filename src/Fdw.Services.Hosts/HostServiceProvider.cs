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
    : DomainServiceProviderBase<
        IHostService,
        IHostImplementationConfiguration,
        IHostFactory<IHostService, IHostImplementationConfiguration>,
        IHostConfigurationProvider>,
      IHostServiceProvider
{
    /// <summary>Initializes a new instance of the <see cref="HostServiceProvider"/> class.</summary>
    /// <param name="logger">The logger for this provider.</param>
    public HostServiceProvider(ILogger<HostServiceProvider> logger)
        : base(logger ?? NullLogger<HostServiceProvider>.Instance)
    {
    }
}
