using Fdw.Collections.Attributes;

namespace Fdw.Services.Messaging;

/// <summary>Worth doing in the ordinary course of work</summary>
[TypeOption(typeof(UiFeedbackUrgencies), "Normal")]
public sealed class NormalUiFeedbackUrgency : UiFeedbackUrgencyBase
{
    /// <summary>Initializes a new instance of the <see cref="NormalUiFeedbackUrgency"/> class.</summary>
    public NormalUiFeedbackUrgency() : base("Normal", "Normal", "Worth doing in the ordinary course of work", "info", 2, false, "Info")
    {
    }
}
