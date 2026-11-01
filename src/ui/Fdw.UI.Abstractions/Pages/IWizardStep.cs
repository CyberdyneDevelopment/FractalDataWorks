using Fdw.UI.Abstractions.Components;

namespace Fdw.UI.Abstractions.Pages;

/// <summary>
/// Represents a single step in a wizard.
/// </summary>
public interface IWizardStep
{
    /// <summary>
    /// Gets the step identifier.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Gets the step title.
    /// </summary>
    string Title { get; }

    /// <summary>
    /// Gets the step description/instructions.
    /// </summary>
    string? Description { get; }

    /// <summary>
    /// Gets the step number (1-based display).
    /// </summary>
    int StepNumber { get; }

    /// <summary>
    /// Gets the step status.
    /// </summary>
    IWizardStepStatus Status { get; }

    /// <summary>
    /// Gets a value indicating whether this step is optional.
    /// </summary>
    bool IsOptional { get; }

    /// <summary>
    /// Gets the content model for this step (form fields, selections, etc.).
    /// </summary>
    IPageModel? Content { get; }

    /// <summary>
    /// Gets the validation result for this step.
    /// </summary>
    ValidationResult ValidationResult { get; }

    /// <summary>
    /// Gets a value indicating whether this step has been completed.
    /// </summary>
    bool IsComplete { get; }

    /// <summary>
    /// Gets the icon for this step.
    /// </summary>
    string? Icon { get; }
}