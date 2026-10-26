using System.Collections.Generic;

namespace Fdw.Services.Configuration;

/// <summary>The closest rows a search found, closest first, and how many rows match in all.</summary>
/// <param name="Rows">The closest rows, up to the window asked for.</param>
/// <param name="Matches">How many rows the text finds in all.</param>
public sealed record RankedRows(IReadOnlyList<RankedRow> Rows, int Matches);
