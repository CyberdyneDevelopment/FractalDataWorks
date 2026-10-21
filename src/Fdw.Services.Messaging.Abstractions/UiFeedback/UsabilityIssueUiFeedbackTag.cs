using Fdw.Collections.Attributes;

namespace Fdw.Services.Messaging;

/// <summary>A screen is hard to understand or to use</summary>
[TypeOption(typeof(UiFeedbackTags), "UsabilityIssue")]
public sealed class UsabilityIssueUiFeedbackTag : UiFeedbackTagBase
{
    /// <summary>Initializes a new instance of the <see cref="UsabilityIssueUiFeedbackTag"/> class.</summary>
    public UsabilityIssueUiFeedbackTag() : base("UsabilityIssue", "Usability Issue", "A screen is hard to understand or to use", "warning")
    {
    }
}
