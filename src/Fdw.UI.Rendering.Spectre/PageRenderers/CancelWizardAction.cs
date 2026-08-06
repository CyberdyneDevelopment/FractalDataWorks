using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Rendering.Spectre.PageRenderers;

/// <summary>Cancel the wizard.</summary>
[TypeOption(typeof(WizardActions), "Cancel")]
[ExcludeFromCodeCoverage]
public sealed class CancelWizardAction : WizardActionBase
{
    /// <summary>Initializes a new instance of <see cref="CancelWizardAction"/>.</summary>
    public CancelWizardAction() : base(6, "Cancel") { }
}
