namespace Fdw.Services.Data.Graphs;

/// <summary>
/// The configuration of a preview of a Graph data set draft (FDW-5454): how many rows each source gives when the caller states no number.
/// </summary>
/// <remarks>
/// Configuration, never code: the default is declared once on <see cref="GraphDataSetRunnerServiceType"/> and a host changes it at registration or in
/// configuration. A caller's own number is judged as a number, not capped by this one.
/// </remarks>
/// <param name="RowsPerSource">The most rows read from each source when the caller states none.</param>
public sealed record GraphDataSetPreviewOptions(int RowsPerSource);
