using System;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// The limits under which this host streams record responses, as <see cref="StreamRecordsEndpointBase{TRequest}"/> consumes them.
/// </summary>
/// <remarks>
/// Owned by the consumer, as <c>ResponseBufferingOptions</c> is owned by the host middleware that reads it: the
/// ResponseStreaming host option reads its configured row and builds this, so the endpoint needs to know nothing
/// about where the limits are stored. The host implementation contract stays empty; the settings live on the
/// host's own record.
/// <para>
/// There are no defaults. A limit that was never configured is not "unlimited" and not some number the framework
/// picked — it is missing, and a missing limit fails at construction rather than streaming under a value nobody
/// chose.
/// </para>
/// </remarks>
public sealed class ResponseStreamingOptions
{
    /// <summary>Initializes a new instance of the <see cref="ResponseStreamingOptions"/> class.</summary>
    /// <param name="maxRows">The greatest number of records one streamed response may carry.</param>
    /// <param name="maxConcurrentStreams">The number of streams an endpoint runs at once.</param>
    /// <exception cref="ArgumentOutOfRangeException">A limit was zero or less.</exception>
    public ResponseStreamingOptions(long maxRows, int maxConcurrentStreams)
    {
        if (maxRows <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxRows), maxRows, "The row ceiling must be greater than zero; zero would refuse every export.");
        }

        if (maxConcurrentStreams <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxConcurrentStreams), maxConcurrentStreams, "The concurrent-stream limit must be greater than zero; zero would refuse every export.");
        }

        MaxRows = maxRows;
        MaxConcurrentStreams = maxConcurrentStreams;
    }

    /// <summary>Gets the greatest number of records one streamed response may carry.</summary>
    /// <remarks>A response that reaches it is aborted rather than ended cleanly, so it cannot pass for a complete one.</remarks>
    public long MaxRows { get; }

    /// <summary>Gets the number of streams an endpoint runs at once.</summary>
    /// <remarks>
    /// A stream holds its cursor — and so a pooled database connection — for as long as the client takes to read,
    /// so this bounds connections held by slow clients rather than merely requests in flight.
    /// </remarks>
    public int MaxConcurrentStreams { get; }
}
