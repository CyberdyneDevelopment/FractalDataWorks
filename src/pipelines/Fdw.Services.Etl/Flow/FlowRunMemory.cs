namespace Fdw.Services.Etl.Flow;

/// <summary>
/// What one run in flight holds now (FDW-5232), in estimated value bytes (RowSize), not process memory: the bytes waiting in its links, each input counted once, and
/// the bytes its components hold.
/// </summary>
/// <param name="QueuedBytes">The bytes waiting in the run's inputs, written and not yet read.</param>
/// <param name="HeldBytes">The bytes the run's components hold.</param>
public sealed record FlowRunMemory(long QueuedBytes, long HeldBytes)
{
    /// <summary>Gets what the run holds in all: <see cref="QueuedBytes"/> and <see cref="HeldBytes"/> together.</summary>
    public long TotalBytes => QueuedBytes + HeldBytes;
}
