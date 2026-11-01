using System;

namespace Fdw.Operations.Clients.Models;

/// <summary>
/// Represents an entry in the activity timeline.
/// </summary>
public sealed class ActivityEntryPayload
{
    /// <summary>Gets or sets the unique identifier of the activity.</summary>
    public Guid? Id { get; set; }
    /// <summary>Gets or sets the type of activity.</summary>
    public IActivityType? Type { get; set; }
    /// <summary>Gets or sets the activity title.</summary>
    public string Title { get; set; } = "";
    /// <summary>Gets or sets the activity description.</summary>
    public string Description { get; set; } = "";
    /// <summary>Gets or sets the severity level.</summary>
    public string Severity { get; set; } = "info";
    /// <summary>Gets or sets the timestamp of the activity.</summary>
    public DateTimeOffset Timestamp { get; set; }
}
