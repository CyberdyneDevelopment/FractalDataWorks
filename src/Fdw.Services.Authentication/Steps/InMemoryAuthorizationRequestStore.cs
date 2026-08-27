using System;
using System.Collections.Concurrent;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Steps;
using Fdw.Services.Authentication.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Authentication.Steps;

/// <summary>
/// Holds authorization requests in process memory between the redirect and the return.
/// </summary>
/// <remarks>
/// Correct only on one instance, for the same reason the in-memory execution store is: a caller can
/// return to a different node than the one that sent them, and the verifier that node stored is not
/// there. Behind a load balancer this presents as intermittent login failure that looks like the
/// provider's fault.
/// </remarks>
public sealed class InMemoryAuthorizationRequestStore : IAuthorizationRequestStore
{
    private readonly ConcurrentDictionary<string, AuthorizationRequest> _requests = new(StringComparer.Ordinal);
    private readonly ILogger<InMemoryAuthorizationRequestStore> _logger;

    /// <summary>Initializes a new instance of the <see cref="InMemoryAuthorizationRequestStore"/> class.</summary>
    /// <param name="logger">The logger.</param>
    public InMemoryAuthorizationRequestStore(ILogger<InMemoryAuthorizationRequestStore>? logger = null)
        => _logger = logger ?? NullLogger<InMemoryAuthorizationRequestStore>.Instance;

    /// <inheritdoc />
    public IGenericResult Store(string state, AuthorizationRequest request)
    {
        if (string.IsNullOrWhiteSpace(state))
            return GenericResult.Failure(RequestStoreLog.StateMissing(_logger));

        if (request is null)
            return GenericResult.Failure(RequestStoreLog.RequestMissing(_logger));

        return _requests.TryAdd(state, request)
            ? GenericResult.Success()
            : GenericResult.Failure(RequestStoreLog.StateReused(_logger));
    }

    /// <inheritdoc />
    public IGenericResult<AuthorizationRequest> TryConsume(string state)
        => string.IsNullOrWhiteSpace(state)
            ? GenericResult<AuthorizationRequest>.Failure(RequestStoreLog.StateMissing(_logger))
            // TryRemove, so consuming is one operation. A check-then-act pair is a window two
            // concurrent callbacks both pass through, which is the replay this prevents.
            : _requests.TryRemove(state, out var request)
                ? GenericResult<AuthorizationRequest>.Success(request)
                : GenericResult<AuthorizationRequest>.Failure(RequestStoreLog.NoSuchRequest(_logger));
}
