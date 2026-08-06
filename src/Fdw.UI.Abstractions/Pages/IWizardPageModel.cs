using System;
using System.Collections.Generic;

namespace Fdw.UI.Abstractions.Pages;

/// <summary>
/// Represents a multi-step wizard for complex configuration flows.
/// </summary>
/// <remarks>
/// Wizard pages guide users through multi-step processes:
/// - Creating complex configurations (pipelines, datasets)
/// - Initial setup workflows
/// - Import/export operations
/// - Migration or upgrade processes
/// </remarks>
public interface IWizardPageModel
{
    /// <summary>
    /// Gets the unique identifier for this wizard.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Gets the wizard title.
    /// </summary>
    string Title { get; }

    /// <summary>
    /// Gets the wizard description.
    /// </summary>
    string? Description { get; }

    /// <summary>
    /// Gets all steps in the wizard.
    /// </summary>
    IReadOnlyList<IWizardStep> Steps { get; }

    /// <summary>
    /// Gets the current step index (0-based).
    /// </summary>
    int CurrentStepIndex { get; }

    /// <summary>
    /// Gets the current step.
    /// </summary>
    IWizardStep CurrentStep { get; }

    /// <summary>
    /// Gets a value indicating whether the user can go to the previous step.
    /// </summary>
    bool CanGoBack { get; }

    /// <summary>
    /// Gets a value indicating whether the user can proceed to the next step.
    /// </summary>
    bool CanGoNext { get; }

    /// <summary>
    /// Gets a value indicating whether the wizard can be completed.
    /// </summary>
    bool CanComplete { get; }

    /// <summary>
    /// Gets a value indicating whether the wizard allows skipping optional steps.
    /// </summary>
    bool AllowSkipOptional { get; }

    /// <summary>
    /// Gets the completion summary text.
    /// </summary>
    string? CompletionSummary { get; }
}