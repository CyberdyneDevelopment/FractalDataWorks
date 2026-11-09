namespace Fdw.Data.DataSets.Abstractions;

/// <summary>How many data sets have one status (FDW-4488).</summary>
/// <param name="Status">The status.</param>
/// <param name="Count">How many data sets have it.</param>
public sealed record DataSetStatusCount(IDataSetStatus Status, int Count);
