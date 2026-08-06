namespace Fdw.Services.Data.Abstractions;

/// <summary>
/// Identifies a DataSet by name. Used with the target-typed overloads of <see cref="IDataGateway"/>
/// to route a command through the DataSet federation layer.
/// </summary>
/// <param name="DataSet">The DataSet name.</param>
// Why: pure positional record (DTO), auto-generated properties only, no logic
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed record DataSetTarget(string DataSet);
