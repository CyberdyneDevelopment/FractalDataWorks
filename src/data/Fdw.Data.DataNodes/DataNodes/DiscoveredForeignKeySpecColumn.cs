namespace Fdw.Services.Data.DataNodes;

/// <summary>
/// One column pair of a foreign key a schema discovery read: the column of the container that holds the key and the column it references.
/// </summary>
/// <param name="Column">The column on the container that holds the key.</param>
/// <param name="ReferencedColumn">The column on the referenced container.</param>
public sealed record DiscoveredForeignKeySpecColumn(string Column, string ReferencedColumn);
