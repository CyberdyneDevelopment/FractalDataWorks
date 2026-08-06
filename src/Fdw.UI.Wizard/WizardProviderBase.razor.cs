using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;

namespace Fdw.UI.Wizard;

/// <summary>
/// Base class for headless wizard provider components. Owns step navigation,
/// loading/saving/error state, first-render initialisation, and a
/// <see cref="Run{T}"/> helper that wraps async operations with consistent
/// error handling and state management.
/// </summary>
/// <typeparam name="TContext">
/// The domain-specific immutable context type passed to the consumer
/// <see cref="RenderFragment{TValue}"/>.
/// </typeparam>
// Why: the state machine itself lives in WizardCore<TContext> so CLI hosts
// can drive identical wizard logic without Blazor. WizardProviderBase forwards
// its (historical) protected surface through a private adapter subclass of
// the core. Existing Blazor subclasses (Connection/DataSet/Schedule wizards)
// see no API change — their abstract overrides, protected properties, and
// protected method calls all continue to work via the delegation.
public abstract partial class WizardProviderBase<TContext> : ComponentBase, IAsyncDisposable
    where TContext : class, new()
{
    private readonly CoreAdapter _core;
    private bool _initialized;

    /// <summary>Initializes the wizard provider, wiring a <see cref="WizardCore{TContext}"/> adapter.</summary>
    protected WizardProviderBase()
    {
        _core = new CoreAdapter(this);
        // Why: StateChanged is an EventHandler (sender/args); StateHasChanged is parameter-less.
        // Wrap so the delegate shapes match.
        _core.StateChanged += (_, _) => StateHasChanged();
    }

    /// <summary>Gets or sets the consumer render fragment that receives the wizard context.</summary>
    [Parameter]
    public RenderFragment<TContext>? ChildContent { get; set; }

    // ── Abstract / virtual hooks — preserved on base for existing subclasses ──

    /// <summary>Gets the total number of steps in this wizard.</summary>
    protected abstract int StepCount { get; }

    /// <summary>Called once during initialisation. Set up the logger, API clients, and any synchronous state here.</summary>
    protected abstract void OnWizardInitialized();

    /// <summary>Called once on first render. Load initial data (connection types, data stores, etc.) here.</summary>
    protected abstract Task LoadInitialData(CancellationToken cancellationToken);

    /// <summary>Build and return the domain-specific context. Called after every state change.</summary>
    protected abstract TContext BuildContext();

    /// <summary>Returns the logger instance for this provider.</summary>
    protected abstract ILogger GetLogger();

    /// <summary>Gate fired before advancing from <paramref name="fromStep"/>. Override to add validation.</summary>
    protected virtual Task<bool> OnBeforeNextStep(int fromStep) => Task.FromResult(true);

    /// <summary>Maps an unhandled exception from <see cref="Run"/> to a user-facing error message.</summary>
    protected virtual IGenericResult OnException(Exception ex) => GenericResult.Failure(ExceptionResultExtensions.FlattenException(ex));

    // ── State — forwarded to the core ─────────────────────────────────────

    /// <summary>Gets or sets the current step index (0-based).</summary>
    protected int Step
    {
        get => _core.Step;
        set => _core.Step = value;
    }

    /// <summary>Gets or sets whether the provider is performing an async load operation.</summary>
    protected bool IsLoading
    {
        get => _core.IsLoading;
        set => _core.IsLoading = value;
    }

    /// <summary>Gets or sets whether the provider is performing a save/submit operation.</summary>
    protected bool IsSaving
    {
        get => _core.IsSaving;
        set => _core.IsSaving = value;
    }

    /// <summary>Gets or sets the result of the most recent operation.</summary>
    protected IGenericResult? LastResult
    {
        get => _core.LastResult;
        set => _core.LastResult = value;
    }

    /// <summary>Gets the current context instance for rendering.</summary>
    protected TContext CurrentContext => _core.CurrentContext;

    /// <summary>Gets a <see cref="CancellationToken"/> that is cancelled when this component is disposed.</summary>
    protected CancellationToken ComponentCt => _core.ComponentCt;

    /// <summary>Gets whether the wizard is on the first step.</summary>
    protected bool IsFirstStep => _core.IsFirstStep;

    /// <summary>Gets whether the wizard is on the last step.</summary>
    protected bool IsLastStep => _core.IsLastStep;

    // ── Step navigation — forwarded to the core ───────────────────────────

    /// <summary>Advance to the next step, subject to <see cref="OnBeforeNextStep"/> validation.</summary>
    protected Task NextStep() => _core.NextStep();

    /// <summary>Return to the previous step, clearing any error.</summary>
    protected void PreviousStep() => _core.PreviousStep();

    /// <summary>Jump to a specific step index, clearing any error.</summary>
    /// <param name="step">The zero-based step index to navigate to.</param>
    protected void GoToStep(int step) => _core.GoToStep(step);

    // ── Async wrappers — forwarded to the core ────────────────────────────

    /// <summary>Runs an async operation with consistent loading state and error handling.</summary>
    /// <param name="operation">The async operation, receiving a <see cref="CancellationToken"/>.</param>
    protected Task Run(Func<CancellationToken, Task> operation) => _core.Run(operation);

    /// <summary>Runs an async operation that returns a value.</summary>
    /// <typeparam name="T">The return type of the operation.</typeparam>
    /// <param name="operation">The async operation.</param>
    /// <returns>The result, or <c>default</c> on failure/cancellation.</returns>
    protected Task<T?> Run<T>(Func<CancellationToken, Task<T?>> operation) => _core.Run(operation);

    /// <summary>Runs an async operation that returns an <see cref="IGenericResult{T}"/>.</summary>
    /// <typeparam name="T">The value type inside the result.</typeparam>
    /// <param name="operation">The async operation.</param>
    /// <returns>The result value on success, or <c>default</c> on failure/cancellation.</returns>
    protected Task<T?> RunResult<T>(Func<CancellationToken, Task<IGenericResult<T>>> operation)
        => _core.RunResult(operation);

    // ── Context management ────────────────────────────────────────────────

    /// <summary>Rebuild the context and notify the renderer.</summary>
    protected void RebuildAndNotify() => _core.RebuildAndNotify();

    /// <summary>Builds a shared <see cref="WizardContext"/> snapshot of the current wizard state.</summary>
    /// <returns>A new <see cref="WizardContext"/> reflecting current state.</returns>
    protected WizardContext BuildWizardContext() => _core.BuildWizardContext();

    // ── Blazor lifecycle — preserved ─────────────────────────────────────

    /// <summary>Sealed lifecycle hook; subclasses use <see cref="OnWizardInitialized"/>.</summary>
    protected sealed override void OnInitialized()
    {
        // Why: call the subclass's OnWizardInitialized synchronously during component
        // init, BEFORE the first render, so subclass-owned state (e.g. ILogger, API
        // clients) is ready when BuildContext runs. Historical behaviour preserved.
        OnWizardInitialized();
        _core.RebuildAndNotify();
    }

    /// <summary>Sealed lifecycle hook; first-render data loading runs via <see cref="LoadInitialData"/>.</summary>
    protected sealed override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && !_initialized)
        {
            _initialized = true;
            WizardProviderLog.InitialDataLoading(GetLogger());
            try
            {
                await LoadInitialData(ComponentCt);
                WizardProviderLog.InitialDataLoaded(GetLogger());
            }
            catch (OperationCanceledException ex)
            {
                // Why: cancellation during initial load is expected on component disposal;
                // ex is named to satisfy FDW022 — no error is surfaced.
                _ = ex;
            }
            catch (Exception ex)
            {
                LastResult = GenericResult.Failure(WizardProviderLog.LoadInitialDataFailed(GetLogger(), ex));
                _core.RebuildAndNotify();
            }
        }
    }

    // ── Dispose ──────────────────────────────────────────────────────────

    /// <summary>Disposes the component by cancelling the <see cref="ComponentCt"/> and releasing the core.</summary>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous dispose.</returns>
    public virtual async ValueTask DisposeAsync()
    {
        await _core.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    // ── Adapter — routes WizardCore abstract methods back to the host ────

    // Why: WizardCore is abstract; we need a concrete subclass so WizardProviderBase
    // can hold an instance. The adapter forwards each abstract/virtual hook from the
    // core to the host's existing abstract/virtual methods, preserving the subclass
    // contract.
    private sealed class CoreAdapter : WizardCore<TContext>
    {
        private readonly WizardProviderBase<TContext> _host;

        public CoreAdapter(WizardProviderBase<TContext> host) => _host = host;

        public override int StepCount => _host.StepCount;

        protected override void OnWizardInitialized() => _host.OnWizardInitialized();

        protected override Task LoadInitialData(CancellationToken cancellationToken)
            => _host.LoadInitialData(cancellationToken);

        protected override TContext BuildContext() => _host.BuildContext();

        protected override ILogger GetLogger() => _host.GetLogger();

        protected override Task<bool> OnBeforeNextStep(int fromStep)
            => _host.OnBeforeNextStep(fromStep);

        protected override IGenericResult OnException(Exception ex) => _host.OnException(ex);
    }
}
