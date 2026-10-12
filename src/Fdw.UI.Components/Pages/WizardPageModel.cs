using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.UI.Abstractions.Pages;

namespace Fdw.UI.Components.Pages;

/// <summary>
/// Concrete implementation of a wizard page model.
/// </summary>
public sealed class WizardPageModel : IWizardPageModel
{
    private readonly List<WizardStep> _steps = [];
    private int _currentStepIndex;

    /// <inheritdoc />
    public string Id { get; set; } = "";

    /// <inheritdoc />
    public string Title { get; set; } = "";

    /// <inheritdoc />
    public string? Description { get; set; }

    /// <inheritdoc />
    public IReadOnlyList<IWizardStep> Steps => _steps;

    /// <inheritdoc />
    public int CurrentStepIndex
    {
        get => _currentStepIndex;
        private set => _currentStepIndex = Math.Clamp(value, 0, Math.Max(0, _steps.Count - 1));
    }

    /// <inheritdoc />
    public IWizardStep CurrentStep => _steps.Count > 0 ? _steps[CurrentStepIndex] : throw new InvalidOperationException("No steps defined");

    /// <inheritdoc />
    public bool CanGoBack => CurrentStepIndex > 0;

    /// <inheritdoc />
    public bool CanGoNext => CurrentStepIndex < _steps.Count - 1 && (CurrentStep.IsComplete || CurrentStep.IsOptional);

    /// <inheritdoc />
    public bool CanComplete => CurrentStepIndex == _steps.Count - 1 && CurrentStep.IsComplete;

    /// <inheritdoc />
    public bool AllowSkipOptional { get; set; } = true;

    /// <inheritdoc />
    public string? CompletionSummary { get; set; }

    /// <summary>
    /// Adds a step to the wizard.
    /// </summary>
    public void AddStep(WizardStep step)
    {
        step.StepNumber = _steps.Count + 1;
        _steps.Add(step);
    }

    /// <summary>
    /// Goes to the previous step.
    /// </summary>
    public bool GoBack()
    {
        if (!CanGoBack) return false;
        _steps[CurrentStepIndex].Status = WizardStepStatuses.Complete;
        CurrentStepIndex--;
        _steps[CurrentStepIndex].Status = WizardStepStatuses.InProgress;
        return true;
    }

    /// <summary>
    /// Goes to the next step.
    /// </summary>
    public bool GoNext()
    {
        if (!CanGoNext) return false;
        _steps[CurrentStepIndex].Status = _steps[CurrentStepIndex].IsComplete
            ? WizardStepStatuses.Complete
            : WizardStepStatuses.Skipped;
        CurrentStepIndex++;
        _steps[CurrentStepIndex].Status = WizardStepStatuses.InProgress;
        return true;
    }

    /// <summary>
    /// Goes to a specific step by index.
    /// </summary>
    public bool GoToStep(int index)
    {
        if (index < 0 || index >= _steps.Count) return false;
        if (index > CurrentStepIndex && !CanGoNext) return false;

        // Mark intermediate steps
        for (int i = CurrentStepIndex; i < index; i++)
        {
            _steps[i].Status = _steps[i].IsComplete ? WizardStepStatuses.Complete : WizardStepStatuses.Skipped;
        }

        CurrentStepIndex = index;
        _steps[CurrentStepIndex].Status = WizardStepStatuses.InProgress;
        return true;
    }

    /// <summary>
    /// Resets the wizard to the first step.
    /// </summary>
    public void Reset()
    {
        foreach (var step in _steps)
        {
            step.Status = WizardStepStatuses.NotStarted;
            step.IsComplete = false;
        }
        CurrentStepIndex = 0;
        if (_steps.Count > 0)
        {
            _steps[0].Status = WizardStepStatuses.InProgress;
        }
    }

    /// <summary>
    /// Gets a step by ID.
    /// </summary>
    public WizardStep? GetStep(string id) => _steps.FirstOrDefault(s => string.Equals(s.Id, id, StringComparison.Ordinal));
}