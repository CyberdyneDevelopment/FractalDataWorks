using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
namespace Fdw.UI.Components.Base;

/// <summary>
/// Base class for headless wizard provider components. Owns step navigation,
/// loading/error/complete state, first-render initialisation, and a
/// <see cref="Run"/> helper that wraps async operations with consistent
/// error handling and state management.
/// </summary>
/// <typeparam name="TContext">
/// The domain-specific immutable context type passed to the consumer
/// <see cref="RenderFragment{TValue}"/>.
/// </typeparam>
public abstract class WizardProviderBase<TContext> : UIComponentBase
    where TContext : class, new()
{
    /// <summary>Gets or sets the consumer render fragment that receives the wizard context.</summary>
    [Parameter] public RenderFragment<TContext>? ChildContent { get; set; }

    // ── Step navigation ────────────────────────────────────────────────────────

    /// <summary>Gets the current step index (0-based).</summary>
    protected int CurrentStep { get; private set; }

    /// <summary>Gets the total number of steps in this wizard.</summary>
    protected abstract int StepCount { get; }

    /// <summary>Gets whether the wizard is on the first step.</summary>
    protected bool IsFirstStep => CurrentStep == 0;

    /// <summary>Gets whether the wizard is on the last step.</summary>
    protected bool IsLastStep => CurrentStep == StepCount - 1;

    // ── Shared state ───────────────────────────────────────────────────────────

    /// <summary>Gets or sets whether the provider is performing an async operation.</summary>
    protected bool IsLoading { get; set; }

    /// <summary>Gets or sets the most recent error message.</summary>
    protected string? ErrorMessage { get; set; }

    /// <summary>Gets or sets whether the wizard has completed successfully.</summary>
    protected bool IsComplete { get; set; }

    /// <summary>Gets the current context instance for rendering.</summary>
    protected TContext CurrentContext { get; private set; } = new();

    private bool _initialized;

    // ── Lifecycle ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Sealed — subclasses use <see cref="OnWizardInitialized"/> for sync init.
    /// </summary>
    protected sealed override void OnInitialized()
    {
        OnWizardInitialized();
        RebuildContext();
    }

    /// <summary>
    /// Sealed — first-render data loading is handled via <see cref="OnInitialLoad"/>.
    /// </summary>
    protected sealed override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && !_initialized)
        {
            _initialized = true;
            await OnInitialLoad(ComponentCt);
        }
    }

    // ── Subclass hooks ─────────────────────────────────────────────────────────

    /// <summary>
    /// Called once during <see cref="OnInitialized"/>. Set up the logger and any
    /// synchronous state here.
    /// </summary>
    protected abstract void OnWizardInitialized();

    /// <summary>
    /// Called once on first render. Load initial data (connection types, data stores, etc.)
    /// here. The base class handles <c>StateHasChanged</c> after this returns.
    /// </summary>
    protected abstract Task OnInitialLoad(CancellationToken cancellationToken);

    /// <summary>
    /// Build and return the domain-specific context. Called after every state change.
    /// </summary>
    protected abstract TContext BuildContext();

    /// <summary>
    /// Called before advancing from <paramref name="fromStep"/> to the next step.
    /// Override to add validation gates (e.g. run a connection test).
    /// Return <c>true</c> to allow the advance, <c>false</c> to block it.
    /// </summary>
    protected virtual Task<bool> OnBeforeNextStep(int fromStep)
    {
        return Task.FromResult(true);
    }

    /// <summary>
    /// Called when an unhandled exception occurs inside <see cref="Run"/>.
    /// Override to log the exception and return a user-facing error message.
    /// The default returns <see cref="Exception.Message"/>.
    /// </summary>
    protected virtual string OnException(Exception ex)
    {
        return ex.Message;
    }

    // ── Step navigation ────────────────────────────────────────────────────────

    /// <summary>Advance to the next step, subject to <see cref="OnBeforeNextStep"/> validation.</summary>
    protected async Task NextStep()
    {
        if (CurrentStep >= StepCount - 1)
        {
            return;
        }

        var allowed = await OnBeforeNextStep(CurrentStep);
        if (allowed && ErrorMessage is null)
        {
            CurrentStep++;
            RebuildAndNotify();
        }
    }

    /// <summary>Return to the previous step, clearing any error.</summary>
    protected void PreviousStep()
    {
        if (CurrentStep <= 0)
        {
            return;
        }

        CurrentStep--;
        ErrorMessage = null;
        RebuildAndNotify();
    }

    /// <summary>Jump to a specific step index.</summary>
    protected void GoToStep(int step)
    {
        if (step < 0 || step >= StepCount)
        {
            return;
        }

        CurrentStep = step;
        ErrorMessage = null;
        RebuildAndNotify();
    }

    // ── Async operation wrapper ────────────────────────────────────────────────

    /// <summary>
    /// Runs an async operation with consistent loading state, error handling,
    /// and context rebuild. Sets <see cref="IsLoading"/> to <c>true</c> before
    /// the operation and <c>false</c> after, regardless of outcome.
    /// </summary>
    protected async Task Run(Func<CancellationToken, Task> operation)
    {
        IsLoading = true;
        ErrorMessage = null;
        RebuildAndNotify();

        try
        {
            await operation(ComponentCt);
        }
        catch (OperationCanceledException ex)
        {
            // Why: cancellation is expected when the component is disposed or the operation is
            // explicitly cancelled; ex is named to satisfy FDW022 — no error is surfaced.
            _ = ex;
        }
        catch (Exception ex)
        {
            ErrorMessage = OnException(ex);
        }
        finally
        {
            IsLoading = false;
            RebuildAndNotify();
        }
    }

    /// <summary>
    /// Runs an async operation that returns a value. Same state management as
    /// <see cref="Run(Func{CancellationToken, Task})"/>.
    /// </summary>
    protected async Task<TResult?> Run<TResult>(Func<CancellationToken, Task<TResult?>> operation)
    {
        IsLoading = true;
        ErrorMessage = null;
        RebuildAndNotify();

        try
        {
            return await operation(ComponentCt);
        }
        catch (OperationCanceledException ex)
        {
            // Why: cancellation is expected; ex is named to satisfy FDW022.
            _ = ex;
            return default;
        }
        catch (Exception ex)
        {
            ErrorMessage = OnException(ex);
            return default;
        }
        finally
        {
            IsLoading = false;
            RebuildAndNotify();
        }
    }

    // ── Context management ─────────────────────────────────────────────────────

    /// <summary>Rebuild the context and notify the renderer.</summary>
    protected void RebuildAndNotify()
    {
        RebuildContext();
        StateHasChanged();
    }

    /// <summary>Rebuild the context without triggering a render.</summary>
    protected void RebuildContext()
    {
        CurrentContext = BuildContext();
    }
}
