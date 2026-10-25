using Fdw.Collections.Attributes;

namespace Fdw.Services.Messaging;

/// <summary>The alert leaves this person's screen and comes back after a short while</summary>
[TypeOption(typeof(AlertActions), "Snooze")]
public sealed class SnoozeAlertAction : AlertActionBase
{
    /// <summary>Initializes a new instance of the <see cref="SnoozeAlertAction"/> class.</summary>
    public SnoozeAlertAction() : base("Snooze", "Remind me", "The alert leaves this person's screen and comes back after a short while", false, null, true, [new AlertActionChoice("quarter-hour", "In 15 minutes", 15), new AlertActionChoice("hour", "In 1 hour", 60), new AlertActionChoice("tomorrow", "Tomorrow", 1440)])
    {
    }
}
