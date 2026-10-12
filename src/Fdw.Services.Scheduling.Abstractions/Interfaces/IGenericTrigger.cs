using System;
using System.Collections.Generic;

namespace Fdw.Services.Scheduling.Abstractions;

/// <summary>
/// Represents a trigger configuration that defines HOW a schedule determines WHEN to execute.
/// </summary>
/// <remarks>
/// A trigger provides the specific configuration and parameters needed by a trigger type
/// to calculate execution times and validate scheduling rules. This separates the trigger
/// logic (in TriggerTypeBase) from the trigger data/configuration.
/// </remarks>
public interface IGenericTrigger
{
    /// <summary>
    /// Gets the unique identifier for this trigger instance.
    /// </summary>
    /// <value>A unique identifier for the trigger.</value>
    string TriggerId { get; }

    /// <summary>
    /// Gets the name of the trigger for display and logging purposes.
    /// </summary>
    /// <value>A human-readable name for the trigger.</value>
    string TriggerName { get; }

    /// <summary>
    /// Gets the trigger type that defines how this trigger calculates execution times.
    /// </summary>
    /// <value>The trigger type that will process this trigger configuration.</value>
    string TriggerType { get; }

    /// <summary>
    /// Gets the configuration parameters specific to the trigger type.
    /// </summary>
    /// <value>A dictionary of configuration parameters used by the trigger type.</value>
    /// <remarks>
    /// For cron triggers, this might contain "Expression" and "TimeZone".
    /// For interval triggers, this might contain "IntervalMinutes" and "StartDelay".
    /// For manual triggers, this might be empty or contain metadata only.
    /// </remarks>
    IReadOnlyDictionary<string, object> Configuration { get; }

    /// <summary>
    /// Gets a value indicating whether this trigger is currently enabled.
    /// </summary>
    /// <value><c>true</c> if the trigger is enabled and can fire; otherwise, <c>false</c>.</value>
    bool IsEnabled { get; }

    /// <summary>
    /// Gets the time when this trigger was created.
    /// </summary>
    /// <value>The UTC timestamp when the trigger was created.</value>
    DateTime CreatedUtc { get; }

    /// <summary>
    /// Gets the time when this trigger was last modified.
    /// </summary>
    /// <value>The UTC timestamp when the trigger was last modified.</value>
    DateTime ModifiedUtc { get; }

    /// <summary>
    /// Gets optional metadata associated with this trigger.
    /// </summary>
    /// <value>A dictionary of key-value pairs containing trigger metadata.</value>
    IReadOnlyDictionary<string, object>? Metadata { get; }
}