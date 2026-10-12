using System.Collections.Generic;

namespace Fdw.Services.Calculations.Abstractions.Sets;

/// <summary>One output row a set could not build: the output it belongs to, the row it was driven by, and why it failed.</summary>
/// <param name="Output">The name of the output the row belongs to.</param>
/// <param name="Row">The values of the row that drove it (a table row, a group or a position), by column name.</param>
/// <param name="Reason">The coded refusal, as text.</param>
public sealed record SetRowFailure(string Output, IReadOnlyDictionary<string, object?> Row, string Reason);
