using Fdw.Abstractions;
using Fdw.Collections;
using Fdw.Configuration;
using Fdw.Services;
using Fdw.Services.Abstractions;
using Fdw.Services.Hosts.Abstractions;
using Fdw.ServiceTypes;

using System.Linq;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
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
    ServiceCategory = "Host")]
public partial class HostTypes : ServiceTypeCollectionBase<
    HostTypeBase<IHostService, IHostImplementationConfiguration, IHostFactory<IHostService, IHostImplementationConfiguration>>,
    IHostType>
{
    /// <summary>
    /// The connection this domain's configuration rows are read from and written to.
    /// </summary>
    public static string ConfigurationConnection { get; set; } = "PlatformConfiguration";

    /// <summary>The Implementation a Host domain row names: Host has exactly one.</summary>
    private const string HostImplementationName = "Host";

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
        // Why this replaces the base's default rather than appending onto it: this class owns its own
        // phase body (the STC001 analyzer enforces that distinction), so the cascade every option's
        // Register() runs as part of has to be reproduced here explicitly, the same way
        // ReferenceEndpoints.Endpoints loops its Groups. What's added at the end is the one thing no
        // option owns: the domain provider itself.
        Registration((builder, loggerFactory) =>
        {
            foreach (var option in Options)
            {
                var result = option.Register(builder, loggerFactory);
                if (result.IsFailure)
                {
                    return result;
                }
            }

            builder.Services.TryAddSingleton<HostImplementationConfigurationProvider>(sp =>
                new HostImplementationConfigurationProvider(
                    sp.GetService<ILogger<HostImplementationConfigurationProvider>>() ?? NullLogger<HostImplementationConfigurationProvider>.Instance,
                    sp.GetRequiredService<IConfigurationGatewayProvider>(), HostTypes.ConfigurationConnection));

            builder.Services.TryAddSingleton<HostConfigurationProvider>(sp =>
            {
                var domain = new HostConfigurationProvider(
                    sp.GetService<ILogger<HostConfigurationProvider>>() ?? NullLogger<HostConfigurationProvider>.Instance,
                    sp.GetRequiredService<IConfigurationGatewayProvider>(), HostTypes.ConfigurationConnection);

                // Host is a single-implementation domain, so one registration under its own name.
                // Without it the domain read finds its row and then has nothing to hand it to.
                domain.Register(HostImplementationName, sp.GetRequiredService<HostImplementationConfigurationProvider>());
                return domain;
            });
            builder.Services.TryAddSingleton<IHostConfigurationProvider>(
                sp => sp.GetRequiredService<HostConfigurationProvider>());

            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });

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
