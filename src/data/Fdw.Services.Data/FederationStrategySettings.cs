namespace Fdw.Services.Data;

/// <summary>
/// The FDW-level values of one federation strategy's options (FDW-6901). A value left null is not stated at this level, so the value the strategy
/// declares on itself applies; a data set's own value wins over both.
/// </summary>
/// <param name="ReadsInParallel">Whether the strategy reads its sources at the same time.</param>
/// <param name="MaxParallelReads">How many sources it reads at once when it reads in parallel; at least one.</param>
/// <param name="OrdersSources">Whether it reads its sources in their stated Priority order.</param>
public sealed record FederationStrategySettings(bool? ReadsInParallel, int? MaxParallelReads, bool? OrdersSources);
