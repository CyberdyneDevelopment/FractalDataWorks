using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Scheduling.Clients.Abstractions;
using Fdw.Services.Scheduling.Abstractions.OptionTypes;
using Fdw.Services.Scheduling.Endpoints.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Scheduling.Endpoints;

/// <summary>
/// Base endpoint that lists all available schedule types from the source-generated TypeCollection.
/// </summary>
/// <remarks>
/// Route: GET /schedules/types
///
/// Why: the generic GET /configuration/types?category=Schedule walks the IDataPath schema-container
/// tree, which matches physical schema names (conn/data/pipe/sched) — not the domain's
/// ServiceCategory string. That means every category except Transform returns zero results.
/// This endpoint reads directly from <see cref="TriggerTypes.All()"/> which is source-generated,
/// reflection-free, and always correct regardless of physical schema topology.
/// </remarks>
public abstract class ListScheduleTypesEndpointBase : EndpointWithoutRequest<List<ScheduleTypeSummary>>
{
    /// <inheritdoc/>
    public override void Configure()
    {
        Get("/schedules/types");
#if DEVELOP
        AllowAnonymous();
#else
        Policies("schedules:read");
#endif
        Summary(s =>
        {
            s.Summary = "List available schedule types";
            s.Description = "Returns all trigger types registered via the source-generated TriggerTypes TypeCollection.";
        });
    }

    /// <inheritdoc/>
    public override Task HandleAsync(CancellationToken ct)
    {
        var endpointLogger = Resolve<ILoggerFactory>().CreateLogger(GetType());

        ScheduleEndpointLog.ListingScheduleTypes(endpointLogger);

        var all = TriggerTypes.All();
        var dtos = new List<ScheduleTypeSummary>(all.Count);

        foreach (var t in all)
        {
            dtos.Add(new ScheduleTypeSummary
            {
                TypeName = t.Name,
                DisplayName = t.Name,
                Description = BuildDescription(t),
                Category = "Schedule"
            });
        }

        ScheduleEndpointLog.ListedScheduleTypes(endpointLogger, dtos.Count);

        return Send.OkAsync(dtos, ct);
    }

    /// <summary>
    /// Builds a human-readable description for a schedule type. Override to provide custom descriptions.
    /// </summary>
    protected virtual string BuildDescription(ITriggerType triggerType) => triggerType.Name switch
    {
        "Cron" => "Time-based scheduling using a cron expression",
        "Interval" => "Recurring execution at a fixed interval (seconds, minutes, hours, or days)",
        "Once" => "Single execution at a specific date and time",
        "Event" => "Execution triggered by a named application event",
        _ => triggerType.Name
    };
}
