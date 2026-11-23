namespace Fdw.Services.Data;

/// <summary>The strategy a federated read runs under and each of its options, with where each value came from (FDW-6901).</summary>
/// <param name="Strategy">The strategy's name: the data set's own, else the FDW-level default.</param>
/// <param name="StrategyFrom">Where the strategy came from: the data set or the FDW-level default.</param>
/// <param name="ReadsInParallel">Whether the sources are read at the same time.</param>
/// <param name="ReadsInParallelFrom">Where ReadsInParallel came from.</param>
/// <param name="MaxParallelReads">How many sources are read at once when they are read in parallel.</param>
/// <param name="MaxParallelReadsFrom">Where MaxParallelReads came from.</param>
/// <param name="OrdersSources">Whether the sources are read in their stated Priority order.</param>
/// <param name="OrdersSourcesFrom">Where OrdersSources came from.</param>
public sealed record ResolvedFederation(
    string Strategy,
    IFederationValueSource StrategyFrom,
    bool ReadsInParallel,
    IFederationValueSource ReadsInParallelFrom,
    int MaxParallelReads,
    IFederationValueSource MaxParallelReadsFrom,
    bool OrdersSources,
    IFederationValueSource OrdersSourcesFrom);
