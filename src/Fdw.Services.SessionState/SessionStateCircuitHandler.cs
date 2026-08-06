using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Services.SessionState.Logging;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.SessionState;

/// <summary>
/// Blazor Server circuit handler that loads user session state when the circuit opens
/// and persists dirty state when the circuit closes.
/// </summary>
public sealed class SessionStateCircuitHandler : CircuitHandler
{
    private readonly ISessionStateService _sessionStateService;
    private readonly AuthenticationStateProvider _authStateProvider;
    private readonly ILogger<SessionStateCircuitHandler> _logger;

    private string? _userId;
    private readonly Dictionary<string, object?> _state = new(StringComparer.Ordinal);
    private readonly HashSet<string> _dirtyKeys = new(StringComparer.Ordinal);

    /// <summary>
    /// Initializes a new instance of the <see cref="SessionStateCircuitHandler"/> class.
    /// </summary>
    /// <param name="sessionStateService">The session state service.</param>
    /// <param name="authStateProvider">The authentication state provider.</param>
    /// <param name="logger">The logger instance.</param>
    public SessionStateCircuitHandler(
        ISessionStateService sessionStateService,
        AuthenticationStateProvider authStateProvider,
        ILogger<SessionStateCircuitHandler> logger)
    {
        _sessionStateService = sessionStateService ?? throw new ArgumentNullException(nameof(sessionStateService));
        _authStateProvider = authStateProvider ?? throw new ArgumentNullException(nameof(authStateProvider));
        _logger = logger ?? NullLogger<SessionStateCircuitHandler>.Instance;
    }

    /// <summary>
    /// Gets the current in-memory session state as a read-only dictionary.
    /// </summary>
    public IReadOnlyDictionary<string, object?> State => _state;

    /// <inheritdoc />
    public override async Task OnConnectionUpAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        SessionStateLog.TraceCircuitOpenedEntry(_logger);

        var authState = await _authStateProvider.GetAuthenticationStateAsync().ConfigureAwait(false);
        var nameIdClaim = authState.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        _userId = nameIdClaim is not null
            ? nameIdClaim
            : authState.User?.FindFirst("sub")?.Value;

        if (!string.IsNullOrEmpty(_userId))
        {
            var keysResult = await _sessionStateService.GetAllKeys(_userId, cancellationToken).ConfigureAwait(false);

            if (keysResult.IsSuccess && keysResult.Value is not null)
            {
                foreach (var key in keysResult.Value)
                {
                    var valueResult = await _sessionStateService.GetState<object>(_userId, key, cancellationToken)
                        .ConfigureAwait(false);

                    if (valueResult.IsSuccess)
                    {
                        _state[key] = valueResult.Value;
                    }
                }

                SessionStateLog.CircuitStateLoaded(_logger, _userId, _state.Count);
            }
        }

        await base.OnConnectionUpAsync(circuit, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public override async Task OnCircuitClosedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        SessionStateLog.TraceCircuitClosedEntry(_logger);

        if (!string.IsNullOrEmpty(_userId) && _dirtyKeys.Count > 0)
        {
            foreach (var key in _dirtyKeys)
            {
                if (_state.TryGetValue(key, out var value))
                {
                    var saveResult = await _sessionStateService.SaveState(_userId, key, value, cancellationToken)
                        .ConfigureAwait(false);

                    if (!saveResult.IsSuccess)
                    {
                        SessionStateLog.SaveStateFailed(_logger, _userId, key, "Failed to persist on circuit close");
                    }
                }
            }

            SessionStateLog.CircuitStatePersisted(_logger, _userId, _dirtyKeys.Count);
            _dirtyKeys.Clear();
        }

        _state.Clear();
        _userId = null;

        await base.OnCircuitClosedAsync(circuit, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Sets a state value in the in-memory cache and marks it as dirty for persistence on circuit close.
    /// </summary>
    /// <param name="key">The state key.</param>
    /// <param name="value">The value to store.</param>
    public void SetState(string key, object? value)
    {
        _state[key] = value;
        _dirtyKeys.Add(key);
    }

    /// <summary>
    /// Removes a state value from the in-memory cache and marks it for deletion on circuit close.
    /// </summary>
    /// <param name="key">The state key to remove.</param>
    public void RemoveState(string key)
    {
        _state.Remove(key);
        _dirtyKeys.Add(key);
    }
}
