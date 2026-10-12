using System.Collections.Generic;

namespace Fdw.Services.Calculations.Abstractions.Sets;

/// <summary>
/// A table a calculation set reads, by the shape it needs: a name and typed columns. It says nothing about where the rows come
/// from; whatever uses the set supplies them.
/// </summary>
/// <param name="Name">The table's name inside the set.</param>
/// <param name="Columns">The columns the set expects every row to carry.</param>
public sealed record TableInputDefinition(string Name, IReadOnlyList<TableColumnDefinition> Columns);
