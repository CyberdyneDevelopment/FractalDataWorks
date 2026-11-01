using Fdw.UI.Abstractions.Components;
using Fdw.UI.Abstractions.Pages;

namespace Fdw.UI.Components.Pages;

/// <summary>
/// Concrete implementation of a wizard step.
/// </summary>
public sealed class WizardStep : IWizardStep
{
    /// <inheritdoc />
    public string Id { get; set; } = "";

    /// <inheritdoc />
    public string Title { get; set; } = "";

    /// <inheritdoc />
    public string? Description { get; set; }

    /// <inheritdoc />
    public int StepNumber { get; internal set; }

    /// <inheritdoc />
    public IWizardStepStatus Status { get; set; } = WizardStepStatuses.NotStarted;

    /// <inheritdoc />
    public bool IsOptional { get; set; }

    /// <inheritdoc />
    public IPageModel? Content { get; set; }

    /// <inheritdoc />
    public ValidationResult ValidationResult { get; set; } = ValidationResult.Success();

    /// <inheritdoc />
    public bool IsComplete { get; set; }

    /// <inheritdoc />
    public string? Icon { get; set; }

    /// <summary>
    /// Validates the step content.
    /// </summary>
    public ValidationResult Validate()
    {
        if (Content == null)
        {
            ValidationResult = ValidationResult.Success();
            IsComplete = true;
            return ValidationResult;
        }

        ValidationResult = Content.Validate();
        IsComplete = ValidationResult.IsValid;
        Status = ValidationResult.IsValid ? WizardStepStatuses.Complete : WizardStepStatuses.Error;
        return ValidationResult;
    }

    /// <summary>
    /// Creates a configuration step with a page model.
    /// </summary>
    public static WizardStep Configure(string id, string title, IPageModel content, string? description = null) =>
        new() { Id = id, Title = title, Description = description, Content = content, Icon = "⚙" };

    /// <summary>
    /// Creates a selection step.
    /// </summary>
    public static WizardStep Select(string id, string title, string? description = null) =>
        new() { Id = id, Title = title, Description = description, Icon = "☐" };

    /// <summary>
    /// Creates a review/summary step.
    /// </summary>
    public static WizardStep Review(string id, string title = "Review", string? description = null) =>
        new() { Id = id, Title = title, Description = description ?? "Review your selections before completing", Icon = "✓" };

    /// <summary>
    /// Creates an optional step.
    /// </summary>
    public static WizardStep Optional(string id, string title, IPageModel? content = null, string? description = null) =>
        new() { Id = id, Title = title, Description = description, Content = content, IsOptional = true, Icon = "○" };
}