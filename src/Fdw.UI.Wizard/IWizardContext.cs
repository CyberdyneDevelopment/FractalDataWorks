using System;
using System.Threading.Tasks;

namespace Fdw.UI.Wizard;

/// <summary>
/// Interface for shared wizard navigation and status state.
/// Composed into domain-specific context objects so that UI markup can bind
/// to a consistent set of wizard properties regardless of the domain.
/// </summary>
public interface IWizardContext
{
    /// <summary>Gets the current step index (0-based).</summary>
    int Step { get; }

    /// <summary>Gets the total number of steps in this wizard.</summary>
    int StepCount { get; }

    /// <summary>Gets whether the wizard is on the first step.</summary>
    bool IsFirstStep { get; }

    /// <summary>Gets whether the wizard is on the last step.</summary>
    bool IsLastStep { get; }

    /// <summary>Gets whether the provider is performing an async load operation.</summary>
    bool IsLoading { get; }

    /// <summary>Gets whether the provider is performing a save/submit operation.</summary>
    bool IsSaving { get; }

    /// <summary>Gets the most recent error message, or <c>null</c> when there is no error.</summary>
    string? ErrorMessage { get; }

    /// <summary>Advances to the next step, subject to validation.</summary>
    Func<Task> OnNextStep { get; }

    /// <summary>Returns to the previous step.</summary>
    Action OnPreviousStep { get; }
}
