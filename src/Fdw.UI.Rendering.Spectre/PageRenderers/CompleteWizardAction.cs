using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Rendering.Spectre.PageRenderers;

/// <summary>Complete the wizard.</summary>
[TypeOption(typeof(WizardActions), "Complete")]
[ExcludeFromCodeCoverage]
public sealed class CompleteWizardAction : WizardActionBase
{
    /// <summary>Initializes a new instance of <see cref="CompleteWizardAction"/>.</summary>
    public CompleteWizardAction() : base(5, "Complete") { }
}
