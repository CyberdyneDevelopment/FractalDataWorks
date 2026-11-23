using System.Threading;
using Fdw.Services.Etl.Abstractions.Flow;

namespace Fdw.Services.Etl.Flow;

/// <summary>
/// What one component of a run holds in memory now and the most it has held (FDW-5231): a component that keeps a whole input, or rows a pump took for it, holds
/// them here and releases them when it lets them go. Every change is an Interlocked operation; the peak is kept with a compare-and-swap.
/// </summary>
/// <remarks>The figures are estimated value bytes (<see cref="RowSize"/>), not process memory.</remarks>
internal sealed class FlowHeldGauge
{
    private long _bytes;
    private long _rows;
    private long _peak;

    /// <summary>Gets what the component holds now and the most it has held.</summary>
    public FlowHeldCount Count => new(Interlocked.Read(ref _bytes), Interlocked.Read(ref _rows), Interlocked.Read(ref _peak));

    /// <summary>Counts a batch as held.</summary>
    /// <param name="batch">The batch the component keeps.</param>
    public void Hold(FlowBatch batch)
    {
        Add(SizeOf(batch), batch.Count);
    }

    /// <summary>Counts a batch as let go.</summary>
    /// <param name="batch">A batch counted by <see cref="Hold"/>.</param>
    public void Release(FlowBatch batch)
    {
        Add(-SizeOf(batch), -batch.Count);
    }

    /// <summary>Lets go of everything the component holds; the peak stays.</summary>
    public void ReleaseAll()
    {
        _ = Interlocked.Exchange(ref _bytes, 0);
        _ = Interlocked.Exchange(ref _rows, 0);
    }

    // The size an output stamped on the batch (FDW-5230); a batch a component built itself carries none and is measured, the same estimate.
    private static long SizeOf(FlowBatch batch) => batch.Bytes is { } stamped ? stamped : RowSize.Of(batch);

    private void Add(long bytes, long rows)
    {
        var now = Interlocked.Add(ref _bytes, bytes);
        _ = Interlocked.Add(ref _rows, rows);
        var peak = Interlocked.Read(ref _peak);
        while (now > peak)
        {
            var seen = Interlocked.CompareExchange(ref _peak, now, peak);
            if (seen == peak)
            {
                return;
            }

            peak = seen;
        }
    }
}
