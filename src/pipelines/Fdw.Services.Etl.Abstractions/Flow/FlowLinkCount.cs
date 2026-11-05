namespace Fdw.Services.Etl.Abstractions.Flow;

/// <summary>
/// What moved over one link in a run (FDW-1386): the row count SSIS shows on a path.
/// </summary>
/// <param name="Link">The link, or for a dropped output the link whose target is the empty string.</param>
/// <param name="Rows">The number of rows written to the link.</param>
/// <param name="Batches">The number of batches written to the link.</param>
/// <param name="Bytes">The estimated size of the rows written to the link (<see cref="RowSize"/>): an estimate of the memory their values hold, not a size on disk or on the wire.</param>
public sealed record FlowLinkCount(FlowLink Link, long Rows, long Batches, long Bytes);
