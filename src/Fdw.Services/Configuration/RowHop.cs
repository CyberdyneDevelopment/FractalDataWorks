namespace Fdw.Services.Configuration;

/// <summary>One step from a row to the row it hangs from: the container read, the column that names what is found, the column that points on.</summary>
/// <param name="Container">The container the step reads, by the key the row before it points at.</param>
/// <param name="NameColumn">The column whose value is a name to report; none for a row that only points on.</param>
/// <param name="NextColumn">The column that holds the key of the next row; none at the end of the chain.</param>
public sealed record RowHop(string Container, string? NameColumn, string? NextColumn);
