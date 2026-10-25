using Fdw.Collections.Attributes;

namespace Fdw.Services.Messaging;

/// <summary>The person has seen the alert and taken it in; it stays on their screen</summary>
[TypeOption(typeof(AlertActions), "Acknowledge")]
public sealed class AcknowledgeAlertAction : AlertActionBase
{
    /// <summary>Initializes a new instance of the <see cref="AcknowledgeAlertAction"/> class.</summary>
    public AcknowledgeAlertAction() : base("Acknowledge", "Acknowledge", "The person has seen the alert and taken it in; it stays on their screen", false, null, false, [])
    {
    }
}
