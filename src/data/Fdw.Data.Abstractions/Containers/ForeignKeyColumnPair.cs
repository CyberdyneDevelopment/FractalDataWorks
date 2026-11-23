namespace Fdw.Data.Abstractions;

/// <summary>
/// One column pair of a foreign key a container declares: a column of the container and the column of the container it references.
/// </summary>
/// <param name="KeyName">The foreign key's name.</param>
/// <param name="Column">The column of the container that holds the key.</param>
/// <param name="ReferencedContainer">The container the key references.</param>
/// <param name="ReferencedColumn">The column of the referenced container.</param>
public sealed record ForeignKeyColumnPair(string KeyName, string Column, IDataContainer ReferencedContainer, string ReferencedColumn);
