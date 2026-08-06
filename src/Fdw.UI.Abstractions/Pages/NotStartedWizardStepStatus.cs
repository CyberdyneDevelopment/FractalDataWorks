using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Abstractions.Pages;

/// <summary>Step has not been visited yet.</summary>
[TypeOption(typeof(WizardStepStatuses), "NotStarted")]
[ExcludeFromCodeCoverage]
public sealed class NotStartedWizardStepStatus : WizardStepStatusBase
{
    /// <summary>Initializes a new instance of <see cref="NotStartedWizardStepStatus"/>.</summary>
    public NotStartedWizardStepStatus() : base(1, "NotStarted") { }
}
