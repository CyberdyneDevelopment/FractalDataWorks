namespace Fdw.Services.Etl.Abstractions;

/// <summary>Where one table input of the Built transformation a Compute stage applies gets its rows (FDW-1231).</summary>
/// <param name="TableName">The table input of the transformation this binds.</param>
/// <param name="FromKind">Stage (the rows arriving at the stage), DataSet (the named data set) or Output (a named output an earlier stage wrote).</param>
/// <param name="FromName">The data set or output name for DataSet or Output; null for Stage.</param>
public sealed record ComputeTableSpec(string TableName, string FromKind, string? FromName);
