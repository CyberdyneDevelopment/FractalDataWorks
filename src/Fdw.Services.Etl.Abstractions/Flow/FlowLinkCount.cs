namespace Fdw.Services.Etl.Abstractions.Flow;

/// <summary>
/// What moved over one link in a run (FDW-1386): the row count SSIS shows on a path.
/// </summary>
/// <param name="Link">The link, or for a dropped output the link whose target is the empty string.</param>
/// <param name="Rows">The number of rows written to the link.</param>
/// <param name="Batches">The number of batches written to the link.</param>
public sealed record FlowLinkCount(FlowLink Link, long Rows, long Batches);
