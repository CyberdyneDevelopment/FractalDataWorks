using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Abstractions.Pages;

/// <summary>Step has validation errors.</summary>
[TypeOption(typeof(WizardStepStatuses), "Error")]
[ExcludeFromCodeCoverage]
public sealed class ErrorWizardStepStatus : WizardStepStatusBase
{
    /// <summary>Initializes a new instance of <see cref="ErrorWizardStepStatus"/>.</summary>
    public ErrorWizardStepStatus() : base(5, "Error") { }
}
