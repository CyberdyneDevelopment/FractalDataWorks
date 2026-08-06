using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Abstractions.Pages;

/// <summary>Step has been completed successfully.</summary>
[TypeOption(typeof(WizardStepStatuses), "Complete")]
[ExcludeFromCodeCoverage]
public sealed class CompleteWizardStepStatus : WizardStepStatusBase
{
    /// <summary>Initializes a new instance of <see cref="CompleteWizardStepStatus"/>.</summary>
    public CompleteWizardStepStatus() : base(3, "Complete") { }
}
