using Fdw.Collections.Attributes;

namespace Fdw.Services.Messaging;

/// <summary>This person is not alerted again for a while; the alert comes back when the time is over</summary>
[TypeOption(typeof(AlertActions), "Silence")]
public sealed class SilenceAlertAction : AlertActionBase
{
    /// <summary>Initializes a new instance of the <see cref="SilenceAlertAction"/> class.</summary>
    public SilenceAlertAction() : base("Silence", "Silence", "This person is not alerted again for a while; the alert comes back when the time is over", false, null, true, [new AlertActionChoice("day", "1 day", 1440), new AlertActionChoice("week", "1 week", 10080)])
    {
    }
}
