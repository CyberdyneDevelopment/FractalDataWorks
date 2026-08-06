using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Rendering.Spectre.PageRenderers;

/// <summary>Go to previous step.</summary>
[TypeOption(typeof(WizardActions), "Previous")]
[ExcludeFromCodeCoverage]
public sealed class PreviousWizardAction : WizardActionBase
{
    /// <summary>Initializes a new instance of <see cref="PreviousWizardAction"/>.</summary>
    public PreviousWizardAction() : base(1, "Previous") { }
}
