using System;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Fdw.Results;
using Microsoft.Extensions.Logging;

namespace Fdw.UI.Wizard;

/// <summary>
/// Headless state-machine core for wizard flows. Owns step navigation,
/// loading/saving/error state, and async-operation wrappers. Has no UI
/// dependency — Blazor (<see cref="WizardProviderBase{TContext}"/>) and
/// terminal (CLI) hosts both drive this core.
/// </summary>
/// <typeparam name="TContext">
/// Domain-specific immutable context type the host renders.
/// </typeparam>
// Why: WizardProviderBase carried all the state-machine logic inside a
// Blazor ComponentBase, which forced CLI hosts to either duplicate the
// logic or fake a Blazor lifecycle. Extracting WizardCore means both
// surfaces share the same step navigation, validation gates, and
// error handling. WizardProviderBase now delegates to a core instance;
// CLI callers construct a core directly.
public abstract class WizardCore<TContext> : IAsyncDisposable
    where TContext : class, new()
{
    private readonly CancellationTokenSource _cts = new();

    // ── State ──────────────────────────────────────────────────────────────

    /// <summary>Zero-based step index. Public setter so hosts (Blazor adapter / CLI subclass) can mutate without base-class inheritance.</summary>
    public int Step { get; set; }

    /// <summary>Total number of steps. Implemented by subclass.</summary>
    public abstract int StepCount { get; }

    /// <summary>True while a <see cref="Run(Func{CancellationToken, Task})"/> call is in progress. Public setter preserves the Blazor subclass pattern that manipulates this directly during custom async sequences.</summary>
    public bool IsLoading { get; set; }

    /// <summary>True while a save/submit is in progress. Settable by hosts that drive commit paths.</summary>
    public bool IsSaving { get; set; }

    /// <summary>Result of the most recent operation, or null before the first one runs.</summary>
    public IGenericResult? LastResult { get; set; }

    /// <summary>The current rebuilt context. Updated on every <see cref="RebuildContext"/>.</summary>
    public TContext CurrentContext { get; private set; } = new();

    /// <summary>Token cancelled by <see cref="DisposeAsync"/>.</summary>
    public CancellationToken ComponentCt => _cts.Token;

    /// <summary>True when on the first step.</summary>
    public bool IsFirstStep => Step == 0;

    /// <summary>True when on the last step.</summary>
    public bool IsLastStep => Step >= StepCount - 1;

    /// <summary>
    /// Raised whenever state changes (step / loading / error / context).
    /// Hosts subscribe and re-render. Payload-free because the host
    /// reads state from the core, not from arguments.
    /// </summary>
    // Why: EventHandler (2-arg) matches the framework delegate shape
    // Meziantou analyzers expect for .NET events; the first argument is
    // unused but satisfies the convention.
    public event EventHandler? StateChanged;

    // ── Abstract hooks ─────────────────────────────────────────────────────

    /// <summary>Synchronous initialisation. Called once via <see cref="Start"/> before first render.</summary>
    protected abstract void OnWizardInitialized();

    /// <summary>Async first-render data load. Called once via <see cref="Start"/> after OnWizardInitialized.</summary>
    protected abstract Task LoadInitialData(CancellationToken cancellationToken);

    /// <summary>Builds a fresh context instance reflecting current state.</summary>
    protected abstract TContext BuildContext();

    /// <summary>Returns the logger for this wizard.</summary>
    protected abstract ILogger GetLogger();

    /// <summary>
    /// Gate fired before advancing from <paramref name="fromStep"/>. Returning false
    /// blocks the advance. Default allows advance.
    /// </summary>
    protected virtual Task<bool> OnBeforeNextStep(int fromStep) => Task.FromResult(true);

    /// <summary>Maps an unhandled exception from <see cref="Run"/> to a user-facing error message.</summary>
    protected virtual IGenericResult OnException(Exception ex) => GenericResult.Failure(ExceptionResultExtensions.FlattenException(ex));

    // ── Start / init ───────────────────────────────────────────────────────

    /// <summary>
    /// Runs <see cref="OnWizardInitialized"/> then <see cref="LoadInitialData"/>.
    /// Safe to call once — further calls are no-ops. Returns when initial load completes
    /// (or fails — failure is captured in <see cref="LastResult"/>).
    /// </summary>
    // Why: FDW convention strips the Async suffix even on async methods.
    private bool _started;
    public async Task Start(CancellationToken cancellationToken = default)
    {
        if (_started) return;
        _started = true;

        OnWizardInitialized();
        RebuildAndNotify();

        WizardProviderLog.InitialDataLoading(GetLogger());
        try
        {
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(ComponentCt, cancellationToken);
            await LoadInitialData(linked.Token).ConfigureAwait(false);
            WizardProviderLog.InitialDataLoaded(GetLogger());
        }
        catch (OperationCanceledException ex)
        {
            // Why: cancellation is expected during component disposal or on the provided token;
            // ex is named to satisfy FDW022 — no error is surfaced at this boundary.
            _ = ex;
        }
        catch (Exception ex)
        {
            LastResult = GenericResult.Failure(new[] { WizardProviderLog.LoadInitialDataFailed(GetLogger(), ex) }.Concat(ExceptionResultExtensions.FlattenException(ex)));
            RebuildAndNotify();
        }
    }

    // ── Step navigation ────────────────────────────────────────────────────

    /// <summary>Advances one step, subject to <see cref="OnBeforeNextStep"/>.</summary>
    public async Task NextStep()
    {
        if (Step >= StepCount - 1)
            return;

        WizardProviderLog.ValidationStarted(GetLogger(), Step);
        var allowed = await OnBeforeNextStep(Step).ConfigureAwait(false);
        if (!allowed)
        {
            WizardProviderLog.StepBlockedByValidation(GetLogger(), Step);
            return;
        }

        if (LastResult is { IsFailure: true })
        {
            WizardProviderLog.StepBlockedByValidation(GetLogger(), Step);
            return;
        }

        Step++;
        WizardProviderLog.StepChanged(GetLogger(), Step, StepCount);
        RebuildAndNotify();
    }

    /// <summary>Steps back one, clearing the current error.</summary>
    public void PreviousStep()
    {
        if (Step <= 0) return;

        Step--;
        LastResult = null;
        WizardProviderLog.StepChanged(GetLogger(), Step, StepCount);
        RebuildAndNotify();
    }

    /// <summary>Jumps to an arbitrary step, clearing the current error.</summary>
    public void GoToStep(int step)
    {
        if (step < 0 || step >= StepCount) return;

        Step = step;
        LastResult = null;
        WizardProviderLog.StepChanged(GetLogger(), Step, StepCount);
        RebuildAndNotify();
    }

    // ── Async operation wrappers ───────────────────────────────────────────

    /// <summary>Runs an async operation with loading + error-capture scaffolding.</summary>
    public async Task Run(Func<CancellationToken, Task> operation)
    {
        IsLoading = true;
        LastResult = null;
        RebuildAndNotify();

        try
        {
            await operation(ComponentCt).ConfigureAwait(false);
        }
        catch (OperationCanceledException ex)
        {
            // Why: cancellation at this boundary (component disposal or explicit cancel) is expected;
            // ex is named to satisfy FDW022 — no error is surfaced.
            _ = ex;
        }
        catch (Exception ex)
        {
            LastResult = OnException(ex);
            WizardProviderLog.OperationFailed(GetLogger(), ex);
        }
        finally
        {
            IsLoading = false;
            RebuildAndNotify();
        }
    }

    /// <summary>Runs an async operation returning a value, with loading + error-capture scaffolding.</summary>
    public async Task<T?> Run<T>(Func<CancellationToken, Task<T?>> operation)
    {
        IsLoading = true;
        LastResult = null;
        RebuildAndNotify();

        try
        {
            return await operation(ComponentCt).ConfigureAwait(false);
        }
        catch (OperationCanceledException ex)
        {
            // Why: cancellation is expected here; ex named to satisfy FDW022.
            _ = ex;
            return default;
        }
        catch (Exception ex)
        {
            LastResult = OnException(ex);
            WizardProviderLog.OperationFailed(GetLogger(), ex);
            return default;
        }
        finally
        {
            IsLoading = false;
            RebuildAndNotify();
        }
    }

    /// <summary>Runs an async operation returning an <see cref="IGenericResult{T}"/> and unwraps success/failure.</summary>
    public async Task<T?> RunResult<T>(Func<CancellationToken, Task<IGenericResult<T>>> operation)
    {
        IsLoading = true;
        LastResult = null;
        RebuildAndNotify();

        try
        {
            var result = await operation(ComponentCt).ConfigureAwait(false);
            if (!result.IsSuccess)
            {
                LastResult = result;
                return default;
            }
            return result.Value;
        }
        catch (OperationCanceledException ex)
        {
            // Why: cancellation is expected here; ex named to satisfy FDW022.
            _ = ex;
            return default;
        }
        catch (Exception ex)
        {
            LastResult = OnException(ex);
            WizardProviderLog.OperationFailed(GetLogger(), ex);
            return default;
        }
        finally
        {
            IsLoading = false;
            RebuildAndNotify();
        }
    }

    // ── Context management ─────────────────────────────────────────────────

    /// <summary>Rebuilds the context, writes it to <see cref="CurrentContext"/>, and raises <see cref="StateChanged"/>.</summary>
    public void RebuildAndNotify()
    {
        RebuildContext();
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Composes a shared <see cref="WizardContext"/> from current state for domain contexts to wrap.</summary>
    public WizardContext BuildWizardContext()
    {
        return new WizardContext
        {
            Step = Step,
            StepCount = StepCount,
            IsFirstStep = IsFirstStep,
            IsLastStep = IsLastStep,
            IsLoading = IsLoading,
            IsSaving = IsSaving,
            LastResult = LastResult,
            OnNextStep = NextStep,
            OnPreviousStep = PreviousStep,
        };
    }

    private void RebuildContext()
    {
        CurrentContext = BuildContext();
        WizardProviderLog.ContextRebuilt(GetLogger(), Step, StepCount);
    }

    // ── Dispose ────────────────────────────────────────────────────────────

    /// <summary>Cancels <see cref="ComponentCt"/> and disposes the underlying token source.</summary>
    // Why: CS1061 / CA1816 — IAsyncDisposable.DisposeAsync is the framework-
    // required name here; suppressing FDW001 for the interface implementation.
    // GC.SuppressFinalize is appropriate because WizardCore honours the
    // IAsyncDisposable contract, disposing its managed resources.
#pragma warning disable FDW001
    public async ValueTask DisposeAsync()
#pragma warning restore FDW001
    {
        await _cts.CancelAsync().ConfigureAwait(false);
        _cts.Dispose();
        GC.SuppressFinalize(this);
    }
}
