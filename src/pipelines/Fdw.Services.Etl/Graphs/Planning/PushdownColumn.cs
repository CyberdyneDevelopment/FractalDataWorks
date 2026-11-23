using System;

namespace Fdw.Services.Etl.Graphs.Planning;

/// <summary>A field of the rows a pushed part sends on (FDW-6969): its name in the row, and the container and column it is read from.</summary>
/// <param name="Name">The field's name in the row.</param>
/// <param name="Container">The container it is read from.</param>
/// <param name="Column">The column of that container.</param>
/// <param name="ClrType">The CLR type of the column's stated data type; null when it states none.</param>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed record PushdownColumn(string Name, string Container, string Column, Type? ClrType);
