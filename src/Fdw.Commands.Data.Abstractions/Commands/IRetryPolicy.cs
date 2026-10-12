namespace Fdw.Commands.Data.Abstractions;

/// <summary>
/// Defines retry behavior for failed operations.
/// </summary>
public interface IRetryPolicy
{
    /// <summary>
    /// Gets the maximum number of retry attempts.
    /// </summary>
    int MaxRetries { get; }

    /// <summary>
    /// Gets the initial delay before the first retry in milliseconds.
    /// </summary>
    int InitialDelayMs { get; }

    /// <summary>
    /// Gets the multiplier applied to delay after each retry (exponential backoff).
    /// </summary>
    double BackoffMultiplier { get; }

    /// <summary>
    /// Gets the maximum delay between retries in milliseconds.
    /// </summary>
    int MaxDelayMs { get; }
}