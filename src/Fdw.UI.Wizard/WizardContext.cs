using System;
using System.Threading.Tasks;
using Fdw.UI.Providers;

namespace Fdw.UI.Wizard;

/// <summary>
/// Default immutable implementation of <see cref="IWizardContext"/>.
/// Built by <see cref="WizardProviderBase{TContext}"/> during each context rebuild
/// and composed into domain-specific context objects.
/// </summary>
public sealed class WizardContext : ProviderContextBase, IWizardContext
{
    /// <inheritdoc />
    public int Step { get; init; }

    /// <inheritdoc />
    public int StepCount { get; init; }

    /// <inheritdoc />
    public bool IsFirstStep { get; init; }

    /// <inheritdoc />
    public bool IsLastStep { get; init; }


    /// <inheritdoc />
    public bool IsSaving { get; init; }


    /// <inheritdoc />
    public Func<Task> OnNextStep { get; init; } = () => Task.CompletedTask;

    /// <inheritdoc />
    public Action OnPreviousStep { get; init; } = () => { };
}
