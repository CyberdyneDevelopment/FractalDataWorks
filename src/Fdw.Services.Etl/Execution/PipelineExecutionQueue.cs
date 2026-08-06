using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Fdw.Services.Etl.Abstractions.Execution;

namespace Fdw.Services.Etl.Execution;

/// <summary>
/// Channel-based bounded queue for pipeline execution requests.
/// Provides backpressure by returning false when the queue is full.
/// </summary>
public sealed class PipelineExecutionQueue : IPipelineExecutionQueue
{
    // Why: BoundedChannelFullMode.DropWrite causes TryWrite to return false when the channel
    // is full, giving the caller a non-blocking signal to return HTTP 503. We never block
    // the HTTP request thread waiting for queue space.
    private readonly Channel<PipelineExecutionRequest> _channel;

    /// <summary>
    /// Initializes a new instance of <see cref="PipelineExecutionQueue"/> with the specified capacity.
    /// </summary>
    /// <param name="capacity">
    /// Maximum number of pending execution requests. Defaults to 100.
    /// Why 100: prevents unbounded memory growth from request storms while allowing
    /// reasonable burst capacity for a single-pipeline-at-a-time executor.
    /// </param>
    public PipelineExecutionQueue(int capacity = 100)
    {
        _channel = Channel.CreateBounded<PipelineExecutionRequest>(
            new BoundedChannelOptions(capacity)
            {
                // Why DropWrite: TryWrite returns false on full instead of blocking.
                // Endpoints detect the false return and respond HTTP 503 immediately.
                FullMode = BoundedChannelFullMode.DropWrite,
                // Why SingleReader = false: allows future scale-out to multiple consumers.
                SingleReader = false,
                // Why SingleWriter = false: multiple endpoints/scheduler can enqueue concurrently.
                SingleWriter = false
            });
    }

    /// <inheritdoc/>
    public ValueTask<bool> Enqueue(
        PipelineExecutionRequest request,
        CancellationToken cancellationToken = default)
    {
        // Why TryWrite (non-blocking): endpoints must return quickly. If the queue is full,
        // return false so the endpoint responds HTTP 503 -- never block the HTTP request thread.
        return new ValueTask<bool>(_channel.Writer.TryWrite(request));
    }

    /// <summary>
    /// Gets the channel reader for the background service to consume from.
    /// Why internal: only PipelineExecutionBackgroundService (same assembly) reads from the
    /// channel. Endpoints should never read from the queue directly.
    /// </summary>
    internal ChannelReader<PipelineExecutionRequest> Reader => _channel.Reader;
}
