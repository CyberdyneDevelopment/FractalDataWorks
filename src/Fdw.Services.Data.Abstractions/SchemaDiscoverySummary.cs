namespace Fdw.Services.Data.Abstractions;

/// <summary>
/// Summary of schema discovery results.
/// </summary>
// Why: pure positional record (DTO), auto-generated properties only, no logic
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed record SchemaDiscoverySummary(
    int TableCount,
    int ViewCount,
    int TotalColumns,
    string[] SchemaNames);
