using Fdw.Abstractions;
using Fdw.Collections;
using Fdw.Configuration;
using Fdw.Services;
using Fdw.Services.Abstractions;
using Fdw.Services.Hosting.Abstractions;
using Fdw.ServiceTypes;

namespace Fdw.Services.Hosting;

/// <summary>
/// The hosting option set — one option per hosting implementation.
/// </summary>
/// <remarks>
/// Hosting is the earliest domain to come up, so its configuration is read through the
/// <c>ServerConfiguration</c> connection declared in <c>configurationSchema.json</c> rather than
/// <c>PlatformConfiguration</c>: a hosting pipeline has to exist before the platform store is
/// reachable.
/// </remarks>
[ServiceTypeCollection(
    typeof(HostingTypeBase<IHostingService, IHostingImplementationConfiguration, IHostingFactory<IHostingService, IHostingImplementationConfiguration>>),
    typeof(IHostingType),
    typeof(HostingTypes),
    ServiceInterface = typeof(IHostingService),
    ProviderType = typeof(HostingServiceProvider),
    ProviderInterface = typeof(IHostingServiceProvider),
    ServiceCategory = "Hosting")]
public partial class HostingTypes : ServiceTypeCollectionBase<
    HostingTypeBase<IHostingService, IHostingImplementationConfiguration, IHostingFactory<IHostingService, IHostingImplementationConfiguration>>,
    IHostingType>
{
}
