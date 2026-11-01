namespace Fdw.Services.Etl.Graphs.Building;

/// <summary>How a test run of a graph differs from a real run (FDW-1665).</summary>
/// <param name="MaxRowsPerSource">The most rows each source reads.</param>
/// <param name="SkipWrites">Whether the destinations write nothing: they count the rows they receive and load none.</param>
public sealed record GraphTestRun(int MaxRowsPerSource, bool SkipWrites);
