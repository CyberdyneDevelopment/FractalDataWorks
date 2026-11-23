using System;
using Fdw.ServiceTypes;
using Fdw.Services.Pipelines.Hubs;
using Fdw.Services.Pipelines.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Pipelines.Notifications;

/// <summary>
/// How the relay of a host that serves no hub batches its events (FDW-5249): how often it sends, and how many events it holds between sends.
/// </summary>
/// <remarks>
/// Both are declared on the pipeline hub option (<see cref="PipelineHubOption.RelayFlushMilliseconds"/>, <see cref="PipelineHubOption.RelayBufferCapacity"/>),
/// the way its coalescing rate is, and a host changes them at registration with <c>ServiceTypeDefaults.Override</c>. Nothing here assumes a value:
/// one that is missing or not a positive whole number stops the relay from being built, naming the key.
/// </remarks>
public sealed class PipelineEventRelayTuning
{
    /// <summary>Initializes a new instance of the <see cref="PipelineEventRelayTuning"/> class.</summary>
    /// <param name="flushEvery">How often the buffered events are sent; longer than zero.</param>
    /// <param name="bufferCapacity">How many events the buffer holds before it drops its oldest components update; at least one.</param>
    public PipelineEventRelayTuning(TimeSpan flushEvery, int bufferCapacity)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(flushEvery, TimeSpan.Zero);
        ArgumentOutOfRangeException.ThrowIfLessThan(bufferCapacity, 1);
        FlushEvery = flushEvery;
        BufferCapacity = bufferCapacity;
    }

    /// <summary>Gets how often the buffered events are sent.</summary>
    public TimeSpan FlushEvery { get; }

    /// <summary>Gets how many events the buffer holds before it drops its oldest components update.</summary>
    public int BufferCapacity { get; }

    /// <summary>Reads the tuning the pipeline hub declares, or a host's override of it.</summary>
    /// <param name="logger">The logger the refusal is written to.</param>
    /// <returns>The tuning.</returns>
    /// <exception cref="InvalidOperationException">A key is not declared as a positive whole number.</exception>
    public static PipelineEventRelayTuning Read(ILogger logger)
        => new(TimeSpan.FromMilliseconds(Positive(logger, PipelineHubOption.RelayFlushMilliseconds)), Positive(logger, PipelineHubOption.RelayBufferCapacity));

    private static int Positive(ILogger logger, string key)
    {
        if (!ServiceTypeDefaults.TryResolve(new PipelineHubOption(), key, out var declared) || declared is not int value || value <= 0)
        {
            throw new InvalidOperationException(PipelineEventRelayLog.RelayTuningNotDeclared(logger, key, declared?.ToString() ?? string.Empty).Message);
        }

        return value;
    }
}
