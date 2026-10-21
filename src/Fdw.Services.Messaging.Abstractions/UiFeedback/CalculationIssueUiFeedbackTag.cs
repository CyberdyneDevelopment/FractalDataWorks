using Fdw.Collections.Attributes;

namespace Fdw.Services.Messaging;

/// <summary>A number on the screen looks wrong</summary>
[TypeOption(typeof(UiFeedbackTags), "CalculationIssue")]
public sealed class CalculationIssueUiFeedbackTag : UiFeedbackTagBase
{
    /// <summary>Initializes a new instance of the <see cref="CalculationIssueUiFeedbackTag"/> class.</summary>
    public CalculationIssueUiFeedbackTag() : base("CalculationIssue", "Calculation Issue", "A number on the screen looks wrong", "error")
    {
    }
}
