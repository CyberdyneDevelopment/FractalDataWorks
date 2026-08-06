namespace Fdw.Web.Http.Authentication;

using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Web.Http.Authentication.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

/// <summary>
/// Default coordinator that serializes refresh operations via <see cref="SemaphoreSlim"/>
/// and skips redundant refreshes within a configurable cooldown window.
/// </summary>
/// <remarks>
/// <para>
/// In Blazor WASM (single-threaded), async interleaving still causes race conditions:
/// multiple <c>await</c> chains can each see <c>IsTokenExpiring=true</c> and trigger
/// concurrent refresh calls. The semaphore serializes them, and the timestamp guard
/// handles the case where the second caller acquires the gate after the first already refreshed.
/// </para>
/// <para>
/// The cooldown window (default 5 seconds) prevents rapid-fire refresh attempts.
/// If a refresh completed within the window, subsequent callers skip and return success.
/// </para>
/// </remarks>
public sealed class DefaultTokenRefreshCoordinator : ITokenRefreshCoordinator, IDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly TimeSpan _cooldownWindow;
    private readonly ILogger<DefaultTokenRefreshCoordinator> _logger;
    private DateTimeOffset _lastRefreshAt = DateTimeOffset.MinValue;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultTokenRefreshCoordinator"/> class
    /// with the default 5-second cooldown window.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public DefaultTokenRefreshCoordinator(ILogger<DefaultTokenRefreshCoordinator>? logger = null)
        : this(TimeSpan.FromSeconds(5), logger)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultTokenRefreshCoordinator"/> class
    /// with a custom cooldown window.
    /// </summary>
    /// <param name="cooldownWindow">The minimum time between refresh operations.</param>
    /// <param name="logger">The logger.</param>
    public DefaultTokenRefreshCoordinator(TimeSpan cooldownWindow, ILogger<DefaultTokenRefreshCoordinator>? logger = null)
    {
        _cooldownWindow = cooldownWindow;
        _logger = logger ?? NullLogger<DefaultTokenRefreshCoordinator>.Instance;
    }

    /// <inheritdoc />
    public async Task<bool> RefreshOnce(Func<CancellationToken, Task<bool>> refreshFunc, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var elapsed = DateTimeOffset.UtcNow - _lastRefreshAt;
            if (elapsed < _cooldownWindow)
            {
                BearerTokenLog.RefreshSkippedRecentlyCompleted(_logger, (long)elapsed.TotalMilliseconds);
                return true;
            }

            BearerTokenLog.RefreshCoordinatorExecuting(_logger);

            var result = await refreshFunc(cancellationToken).ConfigureAwait(false);

            if (result)
            {
                _lastRefreshAt = DateTimeOffset.UtcNow;
            }

            return result;
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _gate.Dispose();
    }
}
