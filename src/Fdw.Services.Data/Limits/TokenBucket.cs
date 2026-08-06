using System;
using System.Threading;

namespace Fdw.Services.Data.Limits;

/// <summary>
/// Thread-safe token bucket for rate-limiting outbound queries.
///
/// Tokens refill at the configured rate per second up to the configured maximum.
/// A non-zero burst capacity allows short spikes.
/// All state is in-memory and resets on process restart — this is intentional
/// to avoid per-request DB writes at the hot path.
/// </summary>
internal sealed class TokenBucket
{
    private readonly double _tokensPerSecond;
    private readonly double _maxTokens;
    private double _tokens;
    private long _lastRefillTicks;
    // Why: System.Threading.Lock (net9+) is preferred over object for lock statements (MA0158).
    private readonly Lock _lock = new();

    /// <summary>
    /// Initializes a new token bucket.
    /// </summary>
    /// <param name="tokensPerSecond">Refill rate (tokens/second).</param>
    /// <param name="maxTokens">Maximum tokens (burst ceiling).</param>
    public TokenBucket(double tokensPerSecond, double maxTokens)
    {
        _tokensPerSecond = tokensPerSecond;
        _maxTokens = maxTokens;
        _tokens = maxTokens;
        _lastRefillTicks = DateTime.UtcNow.Ticks;
    }

    /// <summary>
    /// Attempts to consume one token without blocking.
    /// </summary>
    /// <returns><c>true</c> if a token was consumed; <c>false</c> if the bucket is empty.</returns>
    public bool TryConsume()
    {
        lock (_lock)
        {
            Refill();
            if (_tokens < 1.0)
                return false;

            _tokens -= 1.0;
            return true;
        }
    }

    /// <summary>
    /// Gets the current token count (approximate, may stale between calls).
    /// </summary>
    public double CurrentTokens
    {
        get
        {
            lock (_lock)
            {
                Refill();
                return _tokens;
            }
        }
    }

    private void Refill()
    {
        long nowTicks = DateTime.UtcNow.Ticks;
        double elapsedSeconds = (nowTicks - _lastRefillTicks) / (double)TimeSpan.TicksPerSecond;
        if (elapsedSeconds <= 0.0)
            return;

        _tokens = Math.Min(_maxTokens, _tokens + elapsedSeconds * _tokensPerSecond);
        _lastRefillTicks = nowTicks;
    }
}
