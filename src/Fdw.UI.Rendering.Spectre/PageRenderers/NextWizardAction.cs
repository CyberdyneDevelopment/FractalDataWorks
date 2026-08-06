using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Rendering.Spectre.PageRenderers;

/// <summary>Go to next step.</summary>
[TypeOption(typeof(WizardActions), "Next")]
[ExcludeFromCodeCoverage]
public sealed class NextWizardAction : WizardActionBase
{
    /// <summary>Initializes a new instance of <see cref="NextWizardAction"/>.</summary>
    public NextWizardAction() : base(2, "Next") { }
}
