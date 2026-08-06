using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Abstractions.Pages;

/// <summary>Step is currently active.</summary>
[TypeOption(typeof(WizardStepStatuses), "InProgress")]
[ExcludeFromCodeCoverage]
public sealed class InProgressWizardStepStatus : WizardStepStatusBase
{
    /// <summary>Initializes a new instance of <see cref="InProgressWizardStepStatus"/>.</summary>
    public InProgressWizardStepStatus() : base(2, "InProgress") { }
}
