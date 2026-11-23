namespace Fdw.Commands.Data.Abstractions;

/// <summary>
/// The names of the defaults a federation strategy declares (FDW-6901): the keys of <see cref="FederationStrategyBase.DeclaredDefaults"/>, of a
/// host's <c>ServiceTypeDefaults.Override</c> for a strategy, of the host's configuration under
/// <c>DataStores:Federation:Strategies:&lt;strategy&gt;</c>, and of a data set's own federation option rows.
/// </summary>
/// <remarks>Why named once: each of those places must spell a key the same way, and a misspelt key would read as "declares none".</remarks>
public static class FederationStrategyDefaults
{
    /// <summary>The key of whether the strategy reads its sources at the same time.</summary>
    public const string ReadsInParallel = "ReadsInParallel";

    /// <summary>The key of how many sources the strategy reads at once when it reads in parallel; at least one.</summary>
    public const string MaxParallelReads = "MaxParallelReads";

    /// <summary>The key of whether the strategy reads its sources in their stated Priority order.</summary>
    public const string OrdersSources = "OrdersSources";
}
