using Fdw.Collections.Attributes;

namespace Fdw.Services.Messaging;

/// <summary>Gets in a person's way; do it soon</summary>
[TypeOption(typeof(UiFeedbackUrgencies), "High")]
public sealed class HighUiFeedbackUrgency : UiFeedbackUrgencyBase
{
    /// <summary>Initializes a new instance of the <see cref="HighUiFeedbackUrgency"/> class.</summary>
    public HighUiFeedbackUrgency() : base("High", "High", "Gets in a person's way; do it soon", "warning", 3, false, "Warning")
    {
    }
}
