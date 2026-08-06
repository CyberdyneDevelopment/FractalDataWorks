using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Rendering.Spectre.PageRenderers;

/// <summary>No action taken.</summary>
[TypeOption(typeof(WizardActions), "None")]
[ExcludeFromCodeCoverage]
public sealed class NoneWizardAction : WizardActionBase
{
    /// <summary>Initializes a new instance of <see cref="NoneWizardAction"/>.</summary>
    public NoneWizardAction() : base(0, "None") { }
}
