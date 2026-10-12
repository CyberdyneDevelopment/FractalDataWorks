using Fdw.Collections;

namespace Fdw.UI.Rendering.Spectre.PageRenderers;

/// <summary>
/// Base class for wizard navigation actions.
/// </summary>
public abstract class WizardActionBase : TypeOptionBase<WizardActionBase>, IWizardAction
{
    /// <summary>
    /// Initializes a new instance of <see cref="WizardActionBase"/>.
    /// </summary>
    protected WizardActionBase(string name) : base(name) { }
}
