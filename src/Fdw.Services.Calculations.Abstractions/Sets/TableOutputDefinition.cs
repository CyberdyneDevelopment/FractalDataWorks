using System.Collections.Generic;

namespace Fdw.Services.Calculations.Abstractions.Sets;

/// <summary>A table a calculation set writes: how many rows it has, and the columns each row is made of.</summary>
/// <param name="Name">The output's name inside the set; whatever uses the set routes it by this name.</param>
/// <param name="Rows">How the rows are decided.</param>
/// <param name="Columns">The columns, in order.</param>
public sealed record TableOutputDefinition(string Name, RowSourceDefinition Rows, IReadOnlyList<OutputColumnDefinition> Columns);
