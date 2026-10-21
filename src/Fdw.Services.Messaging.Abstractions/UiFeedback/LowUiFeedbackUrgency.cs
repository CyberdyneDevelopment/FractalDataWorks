using Fdw.Collections.Attributes;

namespace Fdw.Services.Messaging;

/// <summary>Nice to have; nothing is wrong</summary>
[TypeOption(typeof(UiFeedbackUrgencies), "Low")]
public sealed class LowUiFeedbackUrgency : UiFeedbackUrgencyBase
{
    /// <summary>Initializes a new instance of the <see cref="LowUiFeedbackUrgency"/> class.</summary>
    public LowUiFeedbackUrgency() : base("Low", "Low", "Nice to have; nothing is wrong", "neutral", 1, false, "Info")
    {
    }
}
