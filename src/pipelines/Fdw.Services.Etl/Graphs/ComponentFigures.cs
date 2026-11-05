namespace Fdw.Services.Etl.Graphs;

/// <summary>
/// What one component moved in a run (FDW-3381), worked out from the lines of the run's report.
/// </summary>
/// <param name="RowsIn">The rows that arrived at it over its lines.</param>
/// <param name="RowsOut">The rows it sent on, over its lines and out of outputs that are dropped.</param>
/// <param name="BytesOut">The estimated size of the rows it sent on over its lines.</param>
/// <param name="Rejected">The rows it sent out of its error outputs.</param>
internal sealed record ComponentFigures(long RowsIn, long RowsOut, long BytesOut, long Rejected);
