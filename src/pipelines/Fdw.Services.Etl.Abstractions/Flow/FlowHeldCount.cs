namespace Fdw.Services.Etl.Abstractions.Flow;

/// <summary>
/// What one component of a run holds in memory now and the most it has held (FDW-5231), in estimated value bytes (<see cref="RowSize"/>), not process memory.
/// </summary>
/// <param name="Bytes">The estimated value bytes the component holds now.</param>
/// <param name="Rows">The rows the component holds now.</param>
/// <param name="PeakBytes">The most estimated value bytes the component has held at once during the run.</param>
public sealed record FlowHeldCount(long Bytes, long Rows, long PeakBytes);
