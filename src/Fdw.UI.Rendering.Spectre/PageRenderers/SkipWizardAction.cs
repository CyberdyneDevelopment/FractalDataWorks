using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Rendering.Spectre.PageRenderers;

/// <summary>Skip current step (optional steps only).</summary>
[TypeOption(typeof(WizardActions), "Skip")]
[ExcludeFromCodeCoverage]
public sealed class SkipWizardAction : WizardActionBase
{
    /// <summary>Initializes a new instance of <see cref="SkipWizardAction"/>.</summary>
    public SkipWizardAction() : base(3, "Skip") { }
}
