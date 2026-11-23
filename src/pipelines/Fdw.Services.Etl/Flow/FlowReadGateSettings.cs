namespace Fdw.Services.Etl.Flow;

/// <summary>
/// How the reads that feed a join, lookup or union across data stores are let run (FDW-6970): the federation strategy's resolved options, in the flow
/// engine's own terms.
/// </summary>
/// <param name="MaxConcurrentReads">How many of those reads run at once; at least one. One reads them in turn.</param>
/// <param name="BuildSideFirst">Whether the reads of a join's build side (its right input, a lookup's reference) start before any other.</param>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed record FlowReadGateSettings(int MaxConcurrentReads, bool BuildSideFirst);
