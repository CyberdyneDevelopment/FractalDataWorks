namespace Fdw.Data.DataSets.Abstractions;

/// <summary>
/// The names of the tuning defaults a data set kind declares (FDW-3956): the keys of <see cref="DataSetTypeBase.DeclaredDefaults"/>, read
/// with <c>ServiceTypeDefaults.TryResolve</c> for a value a data set request leaves out.
/// </summary>
/// <remarks>
/// Why named once: the declaring option, the create path and a host's <c>ServiceTypeDefaults.Override</c> must spell a key the same way, and a
/// misspelt key would read as "declares none".
/// </remarks>
public static class DataSetDeclaredDefaults
{
    /// <summary>The key of a new data set's version.</summary>
    public const string Version = "Version";

    /// <summary>The key of whether a data set's caching is on, when its caching block does not say.</summary>
    public const string CachingEnabled = "Caching.Enabled";

    /// <summary>The key of how long a data set's cached result lives, in minutes, when its caching block does not say.</summary>
    public const string CachingDurationMinutes = "Caching.DurationMinutes";

    /// <summary>The key of a data set's cache key pattern, when its caching block does not say.</summary>
    public const string CachingKeyPattern = "Caching.KeyPattern";
}
