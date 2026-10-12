namespace Fdw.UI.Rendering.Spectre.PageRenderers;

/// <summary>
/// Result from rendering a wizard page.
/// </summary>
public sealed class WizardPageResult
{
    /// <summary>
    /// Gets or sets whether the wizard should exit.
    /// </summary>
    public bool ShouldExit { get; set; }

    /// <summary>
    /// Gets or sets the navigation action.
    /// </summary>
    public IWizardAction Action { get; set; } = WizardActions.None;

    /// <summary>
    /// Gets or sets the component to edit (when Action is EditFields).
    /// </summary>
    public Fdw.UI.Abstractions.Components.IComponentModel? EditComponent { get; set; }
}