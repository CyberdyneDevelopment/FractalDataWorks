using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Scheduling.Abstractions.TypeCollections.ScheduleTypeOptions.Options;

/// <summary>
/// Schedule type for event-driven scheduling.
/// </summary>
/// <remarks>
/// Event schedule types require an event name that identifies the external or internal event
/// that triggers execution. The schedule fires when the named event is raised, enabling
/// reactive scheduling patterns rather than time-based polling.
/// </remarks>
[TypeOption(typeof(ScheduleTypes), "Event")]
[ExcludeFromCodeCoverage]
public sealed class EventScheduleType : ScheduleTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EventScheduleType"/> class.
    /// </summary>
    public EventScheduleType()
        : base(
            id: 4,
            name: "Event",
            requiresCronExpression: false,
            requiresIntervalDuration: false,
            requiresOneTimeDateTime: false,
            requiresEventName: true)
    {
    }
}
