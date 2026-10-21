using Fdw.Collections.Attributes;

namespace Fdw.Services.Messaging;

/// <summary>A security problem; every developer is told and none can ignore it until they acknowledge it</summary>
[TypeOption(typeof(UiFeedbackUrgencies), "SecurityIssue")]
public sealed class SecurityIssueUiFeedbackUrgency : UiFeedbackUrgencyBase
{
    /// <summary>Initializes a new instance of the <see cref="SecurityIssueUiFeedbackUrgency"/> class.</summary>
    public SecurityIssueUiFeedbackUrgency() : base("SecurityIssue", "Security Issue", "A security problem; every developer is told and none can ignore it until they acknowledge it", "error", 4, true, "Error")
    {
    }
}
