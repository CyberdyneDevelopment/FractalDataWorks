using System.Collections.Generic;

namespace Fdw.Services.Calculations.Abstractions.Sets;

/// <summary>A calculation that reads tables and writes tables. It names no source and no target: only column shapes and formulas.</summary>
/// <param name="Name">The set's name.</param>
/// <param name="TableInputs">The tables it reads, by shape.</param>
/// <param name="Derived">The totals and lookups its formulas can read.</param>
/// <param name="Outputs">The tables it writes.</param>
public sealed record CalculationSetDefinition(
    string Name,
    IReadOnlyList<TableInputDefinition> TableInputs,
    IReadOnlyList<DerivedValueDefinition> Derived,
    IReadOnlyList<TableOutputDefinition> Outputs);
