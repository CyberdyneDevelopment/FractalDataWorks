using System.Collections.Generic;

namespace Fdw.Services.Etl.Pipelines.Loads;

/// <summary>What the load units of one call wrote (FDW-4988): the rows loaded, and the keys of updates and deletes that matched no row, with a sample of them.</summary>
/// <param name="RowsLoaded">The rows the load commands affected, the keys a load stages left out.</param>
/// <param name="UnmatchedKeys">How many keys of the updates and deletes that report them matched no row.</param>
/// <param name="UnmatchedSample">Up to each command's sample size of those keys.</param>
public sealed record LoadOutcome(int RowsLoaded, long UnmatchedKeys, IReadOnlyList<IReadOnlyDictionary<string, object?>> UnmatchedSample);
