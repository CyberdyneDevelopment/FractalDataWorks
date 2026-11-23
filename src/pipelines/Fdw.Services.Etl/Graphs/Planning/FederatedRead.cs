namespace Fdw.Services.Etl.Graphs.Planning;

/// <summary>
/// A read that feeds a join, lookup or union whose inputs read more than one data store (FDW-6970): it runs under the federation strategy's read gate.
/// </summary>
/// <param name="Component">The read: a container source, a data set source, or a pushed query.</param>
/// <param name="Combiner">The join, lookup or union it feeds.</param>
/// <param name="BuildSide">Whether it feeds the side the combiner holds whole (a join's right input, a lookup's reference).</param>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed record FederatedRead(string Component, string Combiner, bool BuildSide);
