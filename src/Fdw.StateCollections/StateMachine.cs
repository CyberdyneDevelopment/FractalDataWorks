using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Results.Abstractions;
using Fdw.StateCollections.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.StateCollections;

/// <summary>
/// Default <see cref="IStateMachine{TState}"/> implementation. The smart-state graph lives on
/// the state TypeOptions themselves; this engine is the validator + sequencer + audit
/// dispatcher around them. Hold one instance per entity.
/// </summary>
/// <typeparam name="TState">The state interface for this machine's domain.</typeparam>
public sealed class StateMachine<TState> : IStateMachine<TState>
    where TState : IStateOption<TState>
{
    private readonly IStateContext<TState> _context;
    private readonly ILogger<StateMachine<TState>> _logger;
    private TState _current;

    /// <summary>Initializes a state machine bound to one entity's context.</summary>
    public StateMachine(IStateContext<TState> context, ILogger<StateMachine<TState>>? logger = null)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? NullLogger<StateMachine<TState>>.Instance;
        _current = context.CurrentState;
    }

    /// <inheritdoc />
    public TState CurrentState => _current;

    /// <inheritdoc />
    public bool CanProgressTo(TState target) =>
        target is not null && _current.CanProgressTo.Any(s => s.Equals(target));

    /// <inheritdoc />
    public async Task<IGenericResult> ProgressTo(TState target, CancellationToken cancellationToken = default)
    {
        if (target is null) throw new ArgumentNullException(nameof(target));

        if (!CanProgressTo(target))
        {
            return GenericResult.Failure(
                StateMachineResultCodes.ByName("InvalidTransition"),
                ResultDetails.Create().With("Current", _current.Name).With("Target", target.Name));
        }

        var exit = await _current.OnExit(_context, cancellationToken).ConfigureAwait(false);
        if (!exit.IsSuccess) return exit;

        var persist = await _context.PersistState(target, cancellationToken).ConfigureAwait(false);
        if (!persist.IsSuccess) return persist;

        var enter = await target.OnEnter(_context, cancellationToken).ConfigureAwait(false);
        if (!enter.IsSuccess) return enter;

        var from = _current;
        _current = target;
        StateMachineLog.Transitioned(_logger, from.Name, target.Name, _context.CorrelationId.ToString());

        var transition = new StateTransition<TState>(from, target, _context.CorrelationId, DateTimeOffset.UtcNow);
        foreach (var handler in _context.TransitionHandlers)
        {
            var handled = await handler.Handle(transition, cancellationToken).ConfigureAwait(false);
            if (!handled.IsSuccess)
            {
                StateMachineLog.HandlerFailed(_logger, handler.GetType().Name, handled.CurrentMessage);
            }
        }

        return GenericResult.Success();
    }
}
