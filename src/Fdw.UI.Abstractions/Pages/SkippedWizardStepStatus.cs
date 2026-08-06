using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Abstractions.Pages;

/// <summary>Step was skipped.</summary>
[TypeOption(typeof(WizardStepStatuses), "Skipped")]
[ExcludeFromCodeCoverage]
public sealed class SkippedWizardStepStatus : WizardStepStatusBase
{
    /// <summary>Initializes a new instance of <see cref="SkippedWizardStepStatus"/>.</summary>
    public SkippedWizardStepStatus() : base(4, "Skipped") { }
}
