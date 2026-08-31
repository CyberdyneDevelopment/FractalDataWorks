using Fdw.Abstractions;
using Fdw.Collections;
using Fdw.Configuration;
using Fdw.Services;
using Fdw.Services.Abstractions;
using Fdw.Services.Hosts.Abstractions;
using Fdw.ServiceTypes;

namespace Fdw.Services.Hosts;

/// <summary>
/// The hosting option set — one option per hosting implementation.
/// </summary>
/// <remarks>
/// Host is the earliest domain to come up, so its configuration is read through the
/// <c>ServerConfiguration</c> connection declared in <c>configurationSchema.json</c> rather than
/// <c>PlatformConfiguration</c>: a hosting pipeline has to exist before the platform store is
/// reachable.
/// </remarks>
[ServiceTypeCollection(
    typeof(HostTypeBase<IHostService, IHostImplementationConfiguration, IHostFactory<IHostService, IHostImplementationConfiguration>>),
    typeof(IHostType),
    typeof(HostTypes),
    ServiceInterface = typeof(IHostService),
    ProviderType = typeof(HostServiceProvider),
    ProviderInterface = typeof(IHostServiceProvider),
    ServiceCategory = "Host")]
public partial class HostTypes : ServiceTypeCollectionBase<
    HostTypeBase<IHostService, IHostImplementationConfiguration, IHostFactory<IHostService, IHostImplementationConfiguration>>,
    IHostType>
{
}
