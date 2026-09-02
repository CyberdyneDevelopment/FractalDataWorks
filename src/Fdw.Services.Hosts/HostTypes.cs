using Fdw.Abstractions;
using Fdw.Collections;
using Fdw.Configuration;
using Fdw.Services;
using Fdw.Services.Abstractions;
using Fdw.Services.Hosts.Abstractions;
using Fdw.ServiceTypes;

using System.Linq;
using Fdw.Results;
using Microsoft.Extensions.Hosting;
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
    /// <summary>Orders this collection's options by where their middleware belongs.</summary>
    /// <remarks>
    /// The default body cycles options in registration order, which for a request pipeline is
    /// whichever packages a host happened to reference first. Order here is not arbitrary --
    /// forwarded headers must be read before anything asks for the scheme -- so the collection
    /// sorts by the position each option declares. A host installs the whole pipeline by running
    /// this phase once and names no option.
    /// </remarks>
    static HostTypes()
    {
        Initialization((host, loggerFactory) =>
        {
            foreach (var option in Options.OrderBy(o => (o as IHostPipelinePosition)?.PipelinePosition ?? int.MaxValue))
            {
                var result = option.Initialize(host, loggerFactory);
                if (result.IsFailure)
                {
                    return result;
                }
            }

            return GenericResult<IHost>.Success(host);
        });
    }
}
