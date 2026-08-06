using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Services.Scheduling.Abstractions.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Scheduling.Abstractions.OptionTypes.TriggerTypeImplementations;

/// <summary>
/// Event trigger type that executes when a named event is raised.
/// </summary>
/// <remarks>
/// <para>
/// The Event trigger type binds execution to a named event rather than to the clock. It supports:
/// </para>
/// <list type="bullet">
///   <item><description>Execution driven by a named event rather than elapsed time</description></item>
///   <item><description>No automatic scheduling or time-based execution</description></item>
///   <item><description>Immediate execution when the named event is raised</description></item>
///   <item><description>Optional description for trigger context and tracking</description></item>
/// </list>
/// <para>
/// Event triggers never calculate an automatic next execution time, so a polling evaluation
/// loop never finds them due — they execute only when the event named by
/// <see cref="EventNameKey"/> is raised. The event name is required: a trigger without one
/// names no event and can never fire, so validation fails rather than assuming a name.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Event trigger bound to a named event
/// var eventConfig = new Dictionary&lt;string, object&gt;
/// {
///     { "EventName", "NightlyExtractCompleted" },
///     { "Description", "Runs once the nightly extract lands" }
/// };
///
/// // Validate trigger (fails when EventName is absent or blank)
/// var eventTrigger = TriggerTypes.Event;
/// var validationResult = eventTrigger.ValidateTrigger(trigger);
/// var nextExecution = eventTrigger.CalculateNextExecution(trigger, null);
/// // nextExecution is always null for event triggers
/// </code>
/// </example>
[TypeOption(typeof(TriggerTypes), "Event", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class Event : TriggerTypeBase
{
    private readonly ILogger<Event> _logger;

    /// <summary>
    /// Configuration key naming the event this trigger listens for. Required.
    /// </summary>
    public const string EventNameKey = "EventName";

    /// <summary>
    /// Configuration key carrying an optional human-readable description.
    /// </summary>
    public const string DescriptionKey = "Description";

    /// <summary>
    /// Initializes a new instance of the <see cref="Event"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    public Event(ILogger<Event>? logger = null) : base(6, "Event", requiresSchedule: false, isImmediate: true)
    {
        _logger = logger ?? NullLogger<Event>.Instance;
    }

    /// <inheritdoc />
    public override DateTime? CalculateNextExecution(IGenericTrigger trigger, DateTime? lastExecution)
    {
        // Event triggers never auto-schedule - they execute only when their named event is raised
        return null;
    }

    /// <inheritdoc />
    public override IGenericResult ValidateTrigger(IGenericTrigger trigger)
    {
        if (trigger?.Configuration == null)
        {
            return GenericResult.Failure(SchedulingLogger.TriggerConfigurationNull(_logger));
        }

        // Why: the event name IS the binding. Absent, blank, or non-string means the trigger
        // names no event and can never fire, so fail loud rather than accept a dead trigger.
        if (!trigger.Configuration.TryGetValue(EventNameKey, out var eventNameObj) ||
            eventNameObj is not string eventName ||
            string.IsNullOrWhiteSpace(eventName))
        {
            return GenericResult.Failure(SchedulingLogger.EventNameRequired(_logger, EventNameKey));
        }

        if (trigger.Configuration.TryGetValue(DescriptionKey, out var descriptionObj) &&
            descriptionObj != null &&
            descriptionObj is not string)
        {
            return GenericResult.Failure(SchedulingLogger.ConfigurationValueMustBeString(_logger, DescriptionKey));
        }

        return GenericResult.Success();
    }

    /// <inheritdoc />
    public override IGenericResult<DateTimeOffset> GetNextRunTime(IGenericTrigger trigger, DateTime? lastExecution)
    {
        return GenericResult<DateTimeOffset>.Failure(SchedulingLogger.EventTriggerCannotComputeNextRunTime(_logger));
    }
}
