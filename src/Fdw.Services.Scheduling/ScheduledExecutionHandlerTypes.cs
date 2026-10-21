using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Fdw.Abstractions;
using Fdw.Collections;
using Fdw.Results;
using Fdw.Services.Scheduling.Abstractions;
using Fdw.Services.Scheduling.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Scheduling;

/// <summary>
/// The scheduled execution handlers a host can register (FDW-902).
/// </summary>
/// <remarks>
/// Quartz's ScheduledPipelineJob resolves an
/// <see cref="IGenericScheduledExecutionHandler"/> for every fire. This collection is how one gets
/// registered: by package reference and the three-phase lifecycle, never by hand in a host.
/// </remarks>
[ExcludeFromCodeCoverage]
[ServiceTypeCollection(
    typeof(ScheduledExecutionHandlerTypeBase<IGenericScheduledExecutionHandler>),
    typeof(IScheduledExecutionHandlerType),
    typeof(ScheduledExecutionHandlerTypes),
    ServiceCategory = "ScheduledExecutionHandler")]
public partial class ScheduledExecutionHandlerTypes
    : ServiceTypeCollectionBase<ScheduledExecutionHandlerTypeBase<IGenericScheduledExecutionHandler>, IScheduledExecutionHandlerType>
{
    /// <summary>
    /// Requires exactly one registered <see cref="IGenericScheduledExecutionHandler"/>. A scheduler
    /// engine calls this from its Initialization phase.
    /// </summary>
    /// <param name="services">The built host's services.</param>
    /// <param name="schedulerName">The engine asking, for the failure message.</param>
    /// <param name="logger">The logger.</param>
    /// <returns>Success, or a failure naming how many handlers were found.</returns>
    /// <remarks>
    /// Why at startup and from the engine (FDW-902): with none, the engine's job could not be built
    /// until the first schedule fired; with two, the last registration silently won. Only a host that
    /// runs an engine needs a handler, so the engine asks -- a host that merely references the
    /// scheduling packages does not. Resolving the handlers also proves their dependencies are wired.
    /// </remarks>
    public static IGenericResult RequireSingleHandler(IServiceProvider services, string schedulerName, ILogger logger)
    {
        if (services is null) throw new ArgumentNullException(nameof(services));

        using var scope = services.CreateScope();
        var handlers = scope.ServiceProvider.GetServices<IGenericScheduledExecutionHandler>().ToList();

        if (handlers.Count == 0)
            return GenericResult.Failure(SchedulingLog.NoScheduledExecutionHandler(logger, schedulerName));

        if (handlers.Count > 1)
        {
            return GenericResult.Failure(SchedulingLog.MultipleScheduledExecutionHandlers(
                logger, schedulerName, handlers.Count, string.Join(", ", handlers.Select(h => h.GetType().Name))));
        }

        return GenericResult.Success();
    }
}
