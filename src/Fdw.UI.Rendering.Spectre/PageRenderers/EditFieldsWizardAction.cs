using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Rendering.Spectre.PageRenderers;

/// <summary>Edit fields in current step.</summary>
[TypeOption(typeof(WizardActions), "EditFields")]
[ExcludeFromCodeCoverage]
public sealed class EditFieldsWizardAction : WizardActionBase
{
    /// <summary>Initializes a new instance of <see cref="EditFieldsWizardAction"/>.</summary>
    public EditFieldsWizardAction() : base(4, "EditFields") { }
}
