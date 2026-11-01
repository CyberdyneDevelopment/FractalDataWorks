using Fdw.Collections.Attributes;

namespace Fdw.Services.Messaging;

/// <summary>The alert is removed from everyone's screen; it is kept, marked with who discarded it and when</summary>
[TypeOption(typeof(AlertActions), "Discard")]
public sealed class DiscardAlertAction : AlertActionBase
{
    /// <summary>Initializes a new instance of the <see cref="DiscardAlertAction"/> class.</summary>
    public DiscardAlertAction() : base("Discard", "Discard", "The alert is removed from everyone's screen; it is kept, marked with who discarded it and when", true, "alerts:discard", true, [])
    {
    }
}
